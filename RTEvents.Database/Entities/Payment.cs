public class Payment
{
    public Payment(decimal cost,  string paymentDetails, int purchaseId)
    {
        Cost = cost;
        Status = PaymentStatus.Pending;
        PaymentDetails = paymentDetails;
        CreatedAt = DateTimeOffset.UtcNow;
        PurchaseId = purchaseId;
    }

    public int Id { get; private set; }
    public decimal Cost { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string PaymentDetails { get; private set; } = string.Empty; // Placeholder for however payment is resolved in the real world, e.g. credit card info.
    public DateTimeOffset CreatedAt { get; private set; }

    public int PurchaseId { get; private set; }
    public Purchase Purchase { get; private set; } = default!;
}

public enum PaymentStatus
{
    Pending,
    Failed,
    Succeeded,
}