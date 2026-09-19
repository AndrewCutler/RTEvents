public interface ITicketsService
{
    Task<Purchase> PurchaseTicketsAsync(int quantity, int eventId, string paymentDetails, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<TicketAvailability> GetTicketAvailabilityAsync(int eventId, CancellationToken cancellationToken = default);
}