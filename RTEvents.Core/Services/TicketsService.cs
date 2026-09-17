using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class TicketsService : ITicketsService
{
    private readonly RTEventsDbContext _context;

    public TicketsService(RTEventsDbContext context)
    {
        _context = context;
    }

    public async Task<Purchase> PurchaseTicketsAsync(int quantity, int eventId, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new Exception("todo custom exception");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var key = await _context.IdempotencyKeys.FindAsync(idempotencyKey);

            var request = $"purchase-ticket--quantity:{quantity};eventId:{eventId}";
            if (key is not null)
            {
                if (!string.Equals(request, key?.Request))
                {
                    throw new Exception("todo custom exception"); // 409 conflict
                }

                var existingPurchase = await _context.Purchases.FindAsync(key!.PurchaseId);

                return existingPurchase ?? throw new Exception();
            }
            else
            {
                _context.IdempotencyKeys.Add(new IdempotencyKey
                {
                    Key = idempotencyKey,
                    Request = request,
                    // ExpiresAt
                });
            }

            var @event = await _context.Events.FindAsync(eventId);

            if (@event is null)
            {
                throw new Exception("todo custom exception");
            }

            var tickets = @event.HoldTickets(quantity);
            var purchase = new Purchase(tickets.Sum(t => t.Cost), tickets);
            var payment = new Payment(purchase.Total, "TODO: from request", purchase);

            _context.Payments.Add(payment);
            _context.Purchases.Add(purchase);
            // Run SaveChangesAsync() before adding outbox message so we generate payment.Id
            await _context.SaveChangesAsync();
            _context.OutboxMessages.Add(new OutboxMessage
            {
                Message = JsonSerializer.Serialize(payment),
                Status = OutboxMessageStatus.Pending,
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return purchase;
        }
        catch (Exception ex)
        {
            // log
            await transaction.RollbackAsync();

            throw;
        }
    }


    public async Task<TicketAvailability> GetTicketAvailabilityAsync(int eventId)
    {
        var @event = await _context.Events
            .Include(e => e.Tickets)
            .SingleOrDefaultAsync(e => e.Id == eventId);

        if (@event is null)
        {
            throw new EventNotFoundException(eventId);
        }

        var available = @event.Tickets.Count(t => t.AvailabilityStatus == AvailabilityStatus.Available);
        var held = @event.Tickets.Count(t => t.AvailabilityStatus == AvailabilityStatus.Held);
        var sold = @event.Tickets.Count(t => t.AvailabilityStatus == AvailabilityStatus.Sold);

        return new TicketAvailability(eventId, available, held, sold, @event.TicketCapacity);
    }
}
