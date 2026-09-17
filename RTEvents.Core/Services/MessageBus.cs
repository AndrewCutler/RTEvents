using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class MessageBus : IMessageBus
{
    private readonly RTEventsDbContext _context;

    public MessageBus(RTEventsDbContext context)
    {
        _context = context;
    }

    public async Task ProcessAsync()
    {
        var pendingPaymentRequests = await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .ToListAsync();

        await SendPaymentRequestsAsync(pendingPaymentRequests);

        var pendingPaymentResponses = await _context.Messages
            .Where(m => m.ProcessedAt == null)
            .ToListAsync();

        await HandlePaymentResponsesAsync(pendingPaymentResponses);
    }

    private async Task SendPaymentRequestsAsync(List<OutboxMessage> messages)
    {
        foreach (var message in messages)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
            message.Attempts++;

            // Arbitrarily setting max attempts at set.
            if (message.Attempts >= 3)
            {
                message.Status = OutboxMessageStatus.DeadLetter;
                continue;
            }

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
                var payload = JsonSerializer.Deserialize<PaymentResponse>(message.Payload)
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
                // log, custom handling here.
                continue;
            }
        }

        var successfulPayments = await _context.Payments
            .Where(m => successes.Contains(m.Id))
            .ToListAsync();

        var failedPayments = await _context.Payments
            .Where(m => failures.Contains(m.Id))
            .ToListAsync();

        foreach (var payment in successfulPayments)
        {
            payment.MarkSucceeded();
        }

        foreach (var payment in failedPayments)
        {
            payment.MarkFailed();
        }
    }
}