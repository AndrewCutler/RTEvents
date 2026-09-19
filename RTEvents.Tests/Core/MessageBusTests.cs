using System.Text.Json;
using Microsoft.Extensions.Logging;
using RTEvents.Tests.Support;

namespace RTEvents.Tests.Core;

public class MessageBusTests
{
    [Fact]
    public async Task Process_sends_pending_outbox_messages_and_ignores_completed_messages()
    {
        using var db = new MockDatabase();
        var pending = new OutboxMessage { Message = "payment payload", Attempts = 1 };
        var processed = new OutboxMessage { Status = OutboxMessageStatus.Processed };
        var dead = new OutboxMessage { Status = OutboxMessageStatus.DeadLetter };
        db.Context.Object.OutboxMessages = MockDatabase.Set(pending, processed, dead).Object;
        var before = DateTimeOffset.UtcNow;
        await new MessageBus(Mock.Of<ILogger<MessageBus>>(), db.Context.Object).ProcessAsync();
        Assert.Equal(2, pending.Attempts);
        Assert.Equal(OutboxMessageStatus.Processed, pending.Status);
        Assert.InRange(pending.ProcessedAt!.Value, before, DateTimeOffset.UtcNow);
        Mock.Get(db.Context.Object.Messages).Verify(s => s.Add(It.Is<Message>(m => m.Type == MessageType.PaymentRequested && m.Payload == "payment payload")), Times.Once);
        Assert.Equal(0, processed.Attempts);
        Assert.Equal(0, dead.Attempts);
        db.VerifySaved(2);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Process_dead_letters_messages_at_attempt_limit(int attempts)
    {
        using var db = new MockDatabase();
        var message = new OutboxMessage { Attempts = attempts };
        db.Context.Object.OutboxMessages = MockDatabase.Set(message).Object;
        await new MessageBus(Mock.Of<ILogger<MessageBus>>(), db.Context.Object).ProcessAsync();
        Assert.Equal(attempts + 1, message.Attempts);
        Assert.Equal(OutboxMessageStatus.DeadLetter, message.Status);
        Assert.Null(message.ProcessedAt);
        Mock.Get(db.Context.Object.Messages).Verify(s => s.Add(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task Process_applies_success_and_failure_and_only_sells_successful_tickets()
    {
        using var db = new MockDatabase();
        var successful = Payment(1);
        var failed = Payment(2);
        var untouched = Payment(3);
        var soldTicket = Samples.Set(new Ticket(7), nameof(Ticket.Purchase), successful.Purchase);
        var heldTicket = Samples.Set(new Ticket(7), nameof(Ticket.Purchase), failed.Purchase);
        var success = Response(1, true);
        var failure = Response(2, false);
        var alreadyProcessed = Response(3, true);
        var processedAt = DateTimeOffset.UtcNow.AddDays(-1);
        alreadyProcessed.ProcessedAt = processedAt;
        var request = new Message { Type = MessageType.PaymentRequested, Payload = "not a response" };
        db.Context.Object.Messages = MockDatabase.Set(success, failure, alreadyProcessed, request, Response(999, true)).Object;
        db.Context.Object.Payments = MockDatabase.Set(successful, failed, untouched).Object;
        db.Context.Object.Tickets = MockDatabase.Set(soldTicket, heldTicket).Object;
        await new MessageBus(Mock.Of<ILogger<MessageBus>>(), db.Context.Object).ProcessAsync();
        Assert.Equal(PaymentStatus.Succeeded, successful.Status);
        Assert.Equal(PaymentStatus.Failed, failed.Status);
        Assert.Equal(PaymentStatus.Pending, untouched.Status);
        Assert.Equal(AvailabilityStatus.Sold, soldTicket.AvailabilityStatus);
        Assert.Equal(AvailabilityStatus.Held, heldTicket.AvailabilityStatus);
        Assert.NotNull(success.ProcessedAt);
        Assert.NotNull(failure.ProcessedAt);
        Assert.Equal(processedAt, alreadyProcessed.ProcessedAt);
        Assert.Null(request.ProcessedAt);
        db.VerifySaved(2);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task Process_logs_invalid_payload_and_continues_with_valid_response(string payload)
    {
        using var db = new MockDatabase();
        var invalid = new Message { Type = MessageType.PaymentSucceeded, Payload = payload };
        var valid = Response(1, true);
        var payment = Payment(1);
        db.Context.Object.Messages = MockDatabase.Set(invalid, valid).Object;
        db.Context.Object.Payments = MockDatabase.Set(payment).Object;
        var logger = new Mock<ILogger<MessageBus>>();
        await new MessageBus(logger.Object, db.Context.Object).ProcessAsync();
        Assert.Null(invalid.ProcessedAt);
        Assert.NotNull(valid.ProcessedAt);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        VerifyError(logger, typeof(JsonException));
    }

    [Fact]
    public async Task Process_logs_and_rethrows_persistence_error()
    {
        using var db = new MockDatabase();
        var error = new InvalidOperationException("save failed");
        db.Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(error);
        var logger = new Mock<ILogger<MessageBus>>();
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => new MessageBus(logger.Object, db.Context.Object).ProcessAsync()));
        VerifyError(logger, typeof(InvalidOperationException));
    }

    private static Payment Payment(int id)
    {
        var purchase = new Purchase(10m, []);
        var payment = Samples.Set(new Payment(10m, "token", purchase), nameof(global::Payment.Id), id);
        Samples.Set(purchase, nameof(Purchase.Payment), payment);
        return payment;
    }

    private static Message Response(int id, bool success) => new()
    {
        Type = success ? MessageType.PaymentSucceeded : MessageType.PaymentFailed,
        Payload = JsonSerializer.Serialize(new PaymentResponseEvent(id, success))
    };

    private static void VerifyError(Mock<ILogger<MessageBus>> logger, Type type) =>
        logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.Is<Exception?>(e => e != null && e.GetType() == type), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
}
