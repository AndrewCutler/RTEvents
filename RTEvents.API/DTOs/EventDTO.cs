
namespace RTEvents.API.DTOs;

// TODO: navigation properties
public record EventDTO(int Id, string Name, string Description, DateOnly Date, TimeOnly Time, string Timezone, int TicketCapacity)
{
    public static EventDTO FromDomain(Event e)
    {
        return new EventDTO(e.Id, e.Name, e.Description, e.Date, e.Time, e.Timezone, e.TicketCapacity);
    }
}