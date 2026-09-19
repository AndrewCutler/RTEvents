using Microsoft.Extensions.Logging.Abstractions;
using RTEvents.Tests.Support;

namespace RTEvents.Tests.Core;

public class CancellationTests
{
    [Fact]
    public async Task Update_passes_token_to_event_lookup_validation_and_save()
    {
        using var cancellation = new CancellationTokenSource();
        using var db = new MockDatabase();
        var events = MockDatabase.Set(Samples.Event());
        var venues = MockDatabase.Set(new Venue { Id = 2, Capacity = 100 });
        db.Context.Object.Events = events.Object;
        db.Context.Object.Venues = venues.Object;

        await new EventsService(db.Context.Object).UpdateAsync(7, cancellationToken: cancellation.Token);

        events.Verify(s => s.FindAsync(It.Is<object?[]>(keys => Equals(keys[0], 7)), cancellation.Token), Times.Once);
        venues.Verify(s => s.FindAsync(It.Is<object?[]>(keys => Equals(keys[0], 2)), cancellation.Token), Times.Once);
        db.Context.Verify(c => c.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Cancelled_purchase_save_rolls_back_without_cancelled_token_and_does_not_commit()
    {
        using var cancellation = new CancellationTokenSource();
        using var db = new MockDatabase();
        db.Context.Object.Events = MockDatabase.Set(Samples.Event()).Object;
        db.Context.Setup(c => c.SaveChangesAsync(cancellation.Token)).Returns(() =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<int>(cancellation.Token);
        });
        var service = new TicketsService(NullLogger<TicketsService>.Instance, db.Context.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.PurchaseTicketsAsync(1, 7, "payment", "request", cancellation.Token));

        db.Database.Verify(d => d.BeginTransactionAsync(cancellation.Token), Times.Once);
        db.Transaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        db.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
