public interface IEventsService
{
    Task<Event?> GetByIdAsync(int id);
    Task<Event> CreateAsync(string name, string description, DateOnly date, TimeOnly time, string timezone, int ticketCapacity, int venueId);
    Task<Event> UpdateAsync(int id, string? name = null, string? description = null, DateOnly? date = null, TimeOnly? time = null, string? timezone = null, int? venueId = null, int? ticketCapacity = null);
    Task DeleteAsync(int id);
}