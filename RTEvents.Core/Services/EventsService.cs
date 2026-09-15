public class EventsService : IEventsService
{
    private readonly RTEventsDbContext _context;

    public EventsService(RTEventsDbContext context)
    {
        _context = context;
    }

    public Task<Event> CreateAsync(string name, string description, DateOnly date, TimeOnly time, string timezone, int ticketCapacity)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Event?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(int id, string? name, string? description, DateOnly? date, TimeOnly? time, string? timezone, int? ticketCapacity)
    {
        throw new NotImplementedException();
    }
}