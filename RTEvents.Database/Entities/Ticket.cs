public class Ticket
{
    public int Id { get; set; }
    public decimal Cost { get; set; }
    public AvailabilityStatus AvailabilityStatus { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = default!;
}

public enum AvailabilityStatus
{
    Available = 0,
    Held,
    Sold,
}