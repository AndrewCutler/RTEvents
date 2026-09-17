using System.Data;
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
            var payment = new Payment(purchase.Total, "TODO: from request", purchase.Id);

            _context.Payments.Add(payment);
            _context.Purchases.Add(purchase);
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
}