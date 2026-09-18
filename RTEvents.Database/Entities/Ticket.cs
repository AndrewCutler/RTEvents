public class Ticket
{
    public Ticket()
    {
        
    }
    
    public int Id { get; set; }
    public decimal Cost { get; set; }
    public AvailabilityStatus AvailabilityStatus { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = default!;

    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = default!;
}

public enum AvailabilityStatus
{
    Held = 0,
    Sold,
}