public class Event
{
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