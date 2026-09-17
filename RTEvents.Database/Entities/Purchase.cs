public class Purchase
{
    public Purchase(decimal total, IEnumerable<Ticket> tickets)
    {
        Total = total;
        Status = PurchaseStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        Tickets = tickets.ToList();
    }

    public int Id { get; private set; }
    public decimal Total { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public int PaymentId { get; private set; }
    public Payment Payment { get; private set; } = default!;
    public ICollection<Ticket> Tickets { get; private set; } = [];
}

public enum PurchaseStatus
{
    Pending,
    Failed,
    Succeeded,
}