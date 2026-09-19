using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class MessageBus : IMessageBus
{
    private readonly ILogger<MessageBus> _logger;
    private readonly RTEventsDbContext _context;

    public MessageBus(ILogger<MessageBus> logger, RTEventsDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task ProcessAsync()
    {
        try
        {
            var pendingPaymentRequests = await _context.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Pending)
                .ToListAsync();

            await SendPaymentRequestsAsync(pendingPaymentRequests);

            var pendingPaymentResponses = await _context.Messages
                .Where(m => m.ProcessedAt == null &&
                    (m.Type == MessageType.PaymentSucceeded || m.Type == MessageType.PaymentFailed))
                .ToListAsync();

            await HandlePaymentResponsesAsync(pendingPaymentResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encountered an errror when processing messages.");

            throw;
        }
    }

    private async Task SendPaymentRequestsAsync(List<OutboxMessage> messages)
    {
        foreach (var message in messages)
        {
            message.Attempts++;

            // Arbitrarily setting max attempts at 3.
            if (message.Attempts >= 3)
            {
                message.Status = OutboxMessageStatus.DeadLetter;
                continue;
            }

            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedAt = DateTimeOffset.UtcNow;
            _context.Messages.Add(new Message
            {
                Type = MessageType.PaymentRequested,
                Payload = message.Message,
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandlePaymentResponsesAsync(List<Message> messages)
    {
        var successes = new HashSet<int>();
        var failures = new HashSet<int>();

        foreach (var message in messages)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<PaymentResponseEvent>(message.Payload)
                    ?? throw new JsonException();

                if (payload.Success)
                {
                    successes.Add(payload.PaymentId);
                }
                else
                {
                    failures.Add(payload.PaymentId);
                }

                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message payload.");
                // log, custom handling here.
                continue;
            }
        }

        var successfulPayments = await _context.Payments
            .Where(m => successes.Contains(m.Id))
            .ToListAsync();

        var tickets = await _context.Tickets
            .Include(t => t.Purchase)
                .ThenInclude(p => p.Payment)
            .Where(t => successes.Contains(t.Purchase.Payment.Id))
            .ToListAsync();

        var failedPayments = await _context.Payments
            .Where(m => failures.Contains(m.Id))
            .ToListAsync();

        foreach (var payment in successfulPayments)
        {
            payment.MarkSucceeded();
        }

        foreach (var ticket in tickets)
        {
            ticket.MarkSold();
        }

        foreach (var payment in failedPayments)
        {
            payment.MarkFailed();
        }

        await _context.SaveChangesAsync();
    }
}
