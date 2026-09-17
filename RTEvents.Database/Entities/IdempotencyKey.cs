public class IdempotencyKey
{
    public string Key { get; set; } = string.Empty;
    public string Request { get; set; } = string.Empty;
    public int PurchaseId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}