using System.Net;

public class VenueNotFoundException : DomainException
{
    public VenueNotFoundException(int id) : base($"Venue with id {id} not found", HttpStatusCode.NotFound)
    {

    }
}