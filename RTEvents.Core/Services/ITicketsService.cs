public interface ITicketsService
{
    Task<Purchase> PurchaseTicketsAsync(int quantity, int eventId, string? idempotencyKey);
}