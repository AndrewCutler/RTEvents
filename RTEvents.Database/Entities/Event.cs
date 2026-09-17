using System.ComponentModel.DataAnnotations;

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

    public void Update(
        string? name,
        string? description,
        DateOnly? date,
        TimeOnly? time,
        string? timezone,
        int? venueId,
        int? ticketCapacity)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            Description = description;
        }

        if (date is not null)
        {
            Date = date.Value;
        }

        if (time is not null)
        {
            Time = time.Value;
        }

        if (!string.IsNullOrWhiteSpace(timezone))
        {
            Timezone = timezone;
        }

        if (venueId.HasValue)
        {
            if (venueId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(venueId),
                    "Ticket capacity must be greater than zero.");
            }

            VenueId = venueId.Value;
        }

        if (ticketCapacity.HasValue)
        {
            if (ticketCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticketCapacity),
                    "Ticket capacity must be greater than zero.");
            }

            TicketCapacity = ticketCapacity.Value;
        }
    }

    public void Activate()
    {
        Status = EventStatus.Active;
    }

    public void Deactivate()
    {
        Status = EventStatus.Inactive;
    }

    public void Delete()
    {
        Status = EventStatus.Deleted;
    }

    public IEnumerable<Ticket> HoldTickets(int quantity)
    {
        if (quantity <= 0)
        {
            throw new Exception("todo: custom exception");
        }

        if (quantity > AvailableTicketCount)
        {
            throw new Exception("todo: custom exception");
        }

        var tickets = new List<Ticket>();
        for (var i = 0; i < quantity; i++)
        {
            var ticket = new Ticket // TODO: constructor
            {
                EventId = Id,
                AvailabilityStatus = AvailabilityStatus.Held,
            };
            tickets.Add(ticket);
            Tickets.Add(ticket);
        }

        AvailableTicketCount -= tickets.Count;

        return tickets;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateOnly Date { get; private set; }
    public TimeOnly Time { get; private set; }
    public string Timezone { get; private set; } = string.Empty;
    public int TicketCapacity { get; private set; }
    public int AvailableTicketCount { get; private set; }
    public EventStatus Status { get; private set; }
    [Timestamp]
    public byte[] RowVersion { get; private set; } = [];

    public int VenueId { get; private set; }
    public Venue Venue { get; private set; } = default!;

    public ICollection<PricingTier> PricingTiers { get; private set; } = [];
    public ICollection<Ticket> Tickets { get; private set; } = [];
}

public enum EventStatus
{
    Active,
    Inactive,
    Deleted
}