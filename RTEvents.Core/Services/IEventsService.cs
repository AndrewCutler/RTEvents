public interface IEventsService
{
    Task<Event?> GetByIdAsync(int id);
    Task<Event> CreateAsync(string name, string description, DateOnly date, TimeOnly time, string timezone, int ticketCapacity);
    Task UpdateAsync(int id, string? name, string? description, DateOnly? date, TimeOnly? time, string? timezone, int? ticketCapacity);
    Task DeleteAsync(int id);
}