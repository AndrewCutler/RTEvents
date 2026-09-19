using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TicketsService : ITicketsService
{
    private readonly ILogger<TicketsService> _logger;
    private readonly RTEventsDbContext _context;

    public TicketsService(ILogger<TicketsService> logger, RTEventsDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<Purchase> PurchaseTicketsAsync(int quantity, int eventId, string paymentDetails, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new MissingIdempotencyKeyException(nameof(PurchaseTicketsAsync));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var key = await _context.IdempotencyKeys.FindAsync([idempotencyKey], cancellationToken);

            var request = $"purchase-ticket--quantity:{quantity};eventId:{eventId}";
            if (key is not null)
            {
                if (!string.Equals(request, key.Request))
                {
                    throw new MismatchedIdempotencyKeyException(key.Request, request);
                }

                var existingPurchase = await _context.Purchases.FindAsync([key.PurchaseId], cancellationToken);

                return existingPurchase ?? throw new PurchaseNotFoundException(key.PurchaseId);
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

            var @event = await _context.Events.FindAsync([eventId], cancellationToken);

            if (@event is null)
            {
                throw new EventNotFoundException(eventId);
            }

            var tickets = @event.HoldTickets(quantity);
            var purchase = new Purchase(tickets.Sum(t => t.Cost), tickets);
            var payment = new Payment(purchase.Total, paymentDetails, purchase);

            _context.Payments.Add(payment);
            _context.Purchases.Add(purchase);
            // Run SaveChangesAsync() before adding outbox message so we generate payment.Id
            await _context.SaveChangesAsync(cancellationToken);
            _context.OutboxMessages.Add(new OutboxMessage
            {
                Message = JsonSerializer.Serialize(new PaymentRequestedEvent
                {
                    PaymentId = payment.Id,
                    Cost = purchase.Total,
                    PaymentDetails = paymentDetails,
                }),
                Status = OutboxMessageStatus.Pending,
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return purchase;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encountered an error purchasing tickets");
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }


    public async Task<TicketAvailability> GetTicketAvailabilityAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events
            .Include(e => e.Tickets)
            .SingleOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (@event is null)
        {
            throw new EventNotFoundException(eventId);
        }

        var held = @event.Tickets.Count(t => t.AvailabilityStatus == AvailabilityStatus.Held);
        var sold = @event.Tickets.Count(t => t.AvailabilityStatus == AvailabilityStatus.Sold);
        var available = @event.TicketCapacity - (held + sold);

        return new TicketAvailability(eventId, available, held, sold, @event.TicketCapacity);
    }
}
