using Microsoft.Extensions.DependencyInjection;

namespace RTEvents.Tests.Core;

public class OutboxHandlerTests
{
    [Fact]
    public async Task Worker_resolves_bus_in_scope_processes_and_disposes_scope_on_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var bus = new Mock<IMessageBus>();
        bus.Setup(b => b.ProcessAsync()).Callback(cancellation.Cancel).Returns(Task.CompletedTask);
        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider.Setup(p => p.GetService(typeof(IMessageBus))).Returns(bus.Object);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(scopedProvider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(factory.Object);
        using var worker = new TestableOutboxHandler(provider.Object);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.Run(cancellation.Token));
        bus.Verify(b => b.ProcessAsync(), Times.Once);
        factory.Verify(f => f.CreateScope(), Times.Once);
        scope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Worker_does_nothing_when_already_cancelled()
    {
        var provider = new Mock<IServiceProvider>(MockBehavior.Strict);
        using var worker = new TestableOutboxHandler(provider.Object);
        await worker.Run(new CancellationToken(true));
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Worker_propagates_bus_error_and_disposes_scope()
    {
        var error = new InvalidOperationException("processing failed");
        var bus = new Mock<IMessageBus>();
        bus.Setup(b => b.ProcessAsync()).ThrowsAsync(error);
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IMessageBus))).Returns(bus.Object);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        provider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(factory.Object);
        using var worker = new TestableOutboxHandler(provider.Object);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => worker.Run(CancellationToken.None)));
        scope.Verify(s => s.Dispose(), Times.Once);
    }

    private sealed class TestableOutboxHandler(IServiceProvider provider) : OutboxHandler(provider)
    {
        public Task Run(CancellationToken token) => ExecuteAsync(token);
    }
}
