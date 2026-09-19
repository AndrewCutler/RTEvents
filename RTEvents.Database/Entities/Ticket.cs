public class Ticket
{
    private Ticket() {}

    public Ticket(int eventId)
    {
        EventId = eventId;
    }

    public void MarkHeld()
    {
        AvailabilityStatus = AvailabilityStatus.Held;
    }
    
    public void MarkSold()
    {
        AvailabilityStatus = AvailabilityStatus.Sold;
    }
    
    public int Id { get; private set; }
    public decimal Cost { get; private set; }
    public AvailabilityStatus AvailabilityStatus { get; private set; }

    public int EventId { get; private set; }
    public Event Event { get; private set; } = default!;

    public int PurchaseId { get; private set; }
    public Purchase Purchase { get; private set; } = default!;
}

public enum AvailabilityStatus
{
    Held = 0,
    Sold,
}