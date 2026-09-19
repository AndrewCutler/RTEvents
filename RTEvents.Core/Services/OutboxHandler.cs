using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class OutboxHandler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OutboxHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            await messageBus.ProcessAsync(stoppingToken);

            // Ideally this would be configurable.
            await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken);
        }
    }
}