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
        int venueId)
    {
        var @event = new Event(
            name,
            description,
            date,
            time,
            timezone,
            ticketCapacity,
            venueId);

        await ValidateAsync(@event);

        _context.Events.Add(@event);
        await _context.SaveChangesAsync();

        return @event;
    }

    public async Task DeleteAsync(int id)
    {
        var @event = await _context.Events.FindAsync(id);

        if (@event is null)
        {
            throw new EventNotFoundException(id);
        }

        @event.Delete();
        await _context.SaveChangesAsync();
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        var @event = await _context.Events.FindAsync(id);

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
        int? ticketCapacity = null)
    {
        var @event = await _context.Events.FindAsync(id);

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

        await ValidateAsync(@event);
        await _context.SaveChangesAsync();

        return @event;
    }

    private async Task ValidateAsync(Event @event)
    {
        var venue = await _context.Venues.FindAsync(@event.VenueId);
        if (venue is null)
        {
            throw new VenueNotFoundException(@event.VenueId);
        }

        if (venue.Capacity < @event.TicketCapacity)
        {
            throw new Exception("todo: custom domain exception.");
        }
    }
}