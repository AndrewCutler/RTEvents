using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RTEvents.Tests.Support;

namespace RTEvents.Tests.Core;

public class TicketsServiceTests
{
    private static TicketsService Service(MockDatabase db) => new(NullLogger<TicketsService>.Instance, db.Context.Object);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task Purchase_requires_idempotency_key_before_opening_transaction(string? key)
    {
        using var db = new MockDatabase();
        await Assert.ThrowsAsync<MissingIdempotencyKeyException>(() => Service(db).PurchaseTicketsAsync(1, 7, "token", key));
        db.Database.Verify(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        db.VerifySaved(0);
    }

    [Fact]
    public async Task Purchase_holds_tickets_creates_payment_and_outbox_then_commits()
    {
        using var db = new MockDatabase();
        var e = Samples.Event();
        db.Context.Object.Events = MockDatabase.Set(e).Object;
        Payment? payment = null;
        OutboxMessage? message = null;
        IdempotencyKey? key = null;
        Mock.Get(db.Context.Object.Payments).Setup(s => s.Add(It.IsAny<Payment>())).Callback<Payment>(p => payment = p);
        Mock.Get(db.Context.Object.OutboxMessages).Setup(s => s.Add(It.IsAny<OutboxMessage>())).Callback<OutboxMessage>(m => message = m);
        Mock.Get(db.Context.Object.IdempotencyKeys).Setup(s => s.Add(It.IsAny<IdempotencyKey>())).Callback<IdempotencyKey>(k => key = k);
        var saves = 0;
        db.Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            if (++saves == 1) { Assert.Null(message); Samples.Set(payment!, nameof(Payment.Id), 42); }
            return Task.FromResult(1);
        });

        var purchase = await Service(db).PurchaseTicketsAsync(2, 7, "token", "request-1");

        Assert.Equal(2, purchase.Tickets.Count);
        Assert.Equal(8, e.AvailableTicketCount);
        Assert.All(purchase.Tickets, t => Assert.Equal(AvailabilityStatus.Held, t.AvailabilityStatus));
        Assert.Equal(purchase.Tickets.Sum(t => t.Cost), purchase.Total);
        Assert.NotNull(payment);
        Assert.Same(purchase, payment.Purchase);
        Assert.Equal(purchase.Total, payment.Cost);
        Assert.Equal("token", payment.PaymentDetails);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Mock.Get(db.Context.Object.Purchases).Verify(s => s.Add(purchase), Times.Once);
        Assert.NotNull(key);
        Assert.Equal("request-1", key.Key);
        Assert.Equal("purchase-ticket--quantity:2;eventId:7", key.Request);
        Assert.NotNull(message);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        var payload = JsonSerializer.Deserialize<PaymentRequestedEvent>(message.Message)!;
        Assert.Equal(42, payload.PaymentId);
        Assert.Equal(purchase.Total, payload.Cost);
        Assert.Equal("token", payload.PaymentDetails);
        db.VerifySaved(2);
        db.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        db.Transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        db.Transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Purchase_replays_existing_purchase_without_creating_or_saving()
    {
        using var db = new MockDatabase();
        var purchase = Samples.Set(new Purchase(15m, []), nameof(Purchase.Id), 4);
        db.Context.Object.Purchases = MockDatabase.Set(purchase).Object;
        db.Context.Object.IdempotencyKeys = MockDatabase.Set(new IdempotencyKey { Key = "key", Request = "purchase-ticket--quantity:2;eventId:7", PurchaseId = 4 }).Object;
        Assert.Same(purchase, await Service(db).PurchaseTicketsAsync(2, 7, "token", "key"));
        Mock.Get(db.Context.Object.Payments).Verify(s => s.Add(It.IsAny<Payment>()), Times.Never);
        Mock.Get(db.Context.Object.OutboxMessages).Verify(s => s.Add(It.IsAny<OutboxMessage>()), Times.Never);
        db.VerifySaved(0);
        db.Transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData("different request", typeof(MismatchedIdempotencyKeyException))]
    [InlineData("purchase-ticket--quantity:2;eventId:7", typeof(PurchaseNotFoundException))]
    public async Task Purchase_rejects_conflicting_key_or_missing_replayed_purchase(string request, Type errorType)
    {
        using var db = new MockDatabase();
        db.Context.Object.IdempotencyKeys = MockDatabase.Set(new IdempotencyKey { Key = "key", Request = request, PurchaseId = 4 }).Object;
        Assert.IsType(errorType, await Record.ExceptionAsync(() => Service(db).PurchaseTicketsAsync(2, 7, "token", "key")));
        db.VerifySaved(0);
        db.Transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        db.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(99, 1, typeof(EventNotFoundException))]
    [InlineData(7, 0, typeof(Exception))]
    [InlineData(7, -1, typeof(Exception))]
    [InlineData(7, 11, typeof(Exception))]
    public async Task Purchase_rolls_back_when_event_or_quantity_is_invalid(int eventId, int quantity, Type errorType)
    {
        using var db = new MockDatabase();
        db.Context.Object.Events = MockDatabase.Set(Samples.Event()).Object;
        Assert.IsType(errorType, await Record.ExceptionAsync(() => Service(db).PurchaseTicketsAsync(quantity, eventId, "token", "key")));
        db.VerifySaved(0);
        db.Transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        db.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Purchase_rolls_back_and_rethrows_save_failure(int failingSave)
    {
        using var db = new MockDatabase();
        db.Context.Object.Events = MockDatabase.Set(Samples.Event()).Object;
        var error = new InvalidOperationException("Save failed");
        var count = 0;
        db.Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => ++count == failingSave ? Task.FromException<int>(error) : Task.FromResult(1));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).PurchaseTicketsAsync(1, 7, "token", "key")));
        db.Transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        db.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        db.Transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 3)]
    [InlineData(5, 5)]
    public async Task Availability_counts_held_sold_and_remaining_tickets(int held, int sold)
    {
        using var db = new MockDatabase();
        var e = Samples.Event();
        if (held + sold > 0)
        {
            var tickets = e.HoldTickets(held + sold).ToList();
            foreach (var ticket in tickets.Take(sold)) ticket.MarkSold();
        }
        db.Context.Object.Events = MockDatabase.Set(e).Object;
        Assert.Equal(new TicketAvailability(7, 10 - held - sold, held, sold, 10), await Service(db).GetTicketAvailabilityAsync(7));
        db.VerifySaved(0);
    }

    [Fact]
    public async Task Availability_throws_for_missing_event()
    {
        using var db = new MockDatabase();
        await Assert.ThrowsAsync<EventNotFoundException>(() => Service(db).GetTicketAvailabilityAsync(99));
    }
}
