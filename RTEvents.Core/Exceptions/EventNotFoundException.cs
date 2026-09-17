using System.Net;

public class EventNotFoundException : DomainException
{
    public EventNotFoundException(int id) : base($"Event with id ${id} not found", HttpStatusCode.NotFound)
    {

    }
}