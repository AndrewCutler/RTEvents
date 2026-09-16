public class Event
{
    public Event(string name, string description, DateOnly date, TimeOnly time, string timezone, int ticketCapacity, int venueId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name cannot be null or empty.", nameof(name));
        }
        
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Event description cannot be null or empty.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(timezone))
        {
            throw new ArgumentException("Event timezone cannot be null or empty.", nameof(timezone));
        }

        if (ticketCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticketCapacity), "Ticket capacity must be greater than zero.");
        }

        if (venueId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(venueId), "Venue ID must be greater than zero.");
        }

        Name = name;
        Description = description;
        Date = date;
        Time = time;
        Timezone = timezone;
        TicketCapacity = ticketCapacity;
        VenueId = venueId;
    }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public int TicketCapacity { get; set; }

    public int VenueId { get; set; }
    public Venue Venue { get; set; } = default!;

    public ICollection<PricingTier> PricingTiers { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];

}