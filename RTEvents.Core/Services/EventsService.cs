public class EventsService : IEventsService
{
    private readonly RTEventsDbContext _context;

    public EventsService(RTEventsDbContext context)
    {
        _context = context;
    }

    public async Task<Event> CreateAsync(
        string name,
        string description,
        DateOnly date,
        TimeOnly time,
        string timezone,
        int ticketCapacity,
        int venueId,
        CancellationToken cancellationToken = default)
    {
        var @event = new Event(
            name,
            description,
            date,
            time,
            timezone,
            ticketCapacity,
            venueId);

        await ValidateAsync(@event, cancellationToken);

        _context.Events.Add(@event);
        await _context.SaveChangesAsync(cancellationToken);

        return @event;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FindAsync([id], cancellationToken);

        if (@event is null)
        {
            throw new EventNotFoundException(id);
        }

        @event.Delete();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FindAsync([id], cancellationToken);

        return @event;
    }

    public async Task<Event> UpdateAsync(
        int id,
        string? name = null,
        string? description = null,
        DateOnly? date = null,
        TimeOnly? time = null,
        string? timezone = null,
        int? venueId = null,
        int? ticketCapacity = null,
        CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FindAsync([id], cancellationToken);

        if (@event is null)
        {
            throw new EventNotFoundException(id);
        }

        @event.Update(
            name: name,
            description: description,
            date: date,
            time: time,
            timezone: timezone,
            venueId: venueId,
            ticketCapacity: ticketCapacity);

        await ValidateAsync(@event, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return @event;
    }

    private async Task ValidateAsync(Event @event, CancellationToken cancellationToken)
    {
        var venue = await _context.Venues.FindAsync([@event.VenueId], cancellationToken);
        if (venue is null)
        {
            throw new VenueNotFoundException(@event.VenueId);
        }

        if (venue.Capacity < @event.TicketCapacity)
        {
            throw new EventOverCapacityException(@event.TicketCapacity);
        }
    }
}