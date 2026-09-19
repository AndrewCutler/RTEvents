using System.Net;

public class MissingIdempotencyKeyException : DomainException
{
    public MissingIdempotencyKeyException(string methodName) : base($"An idempotency key must be provided for {methodName}.", HttpStatusCode.BadRequest)
    {

    }
}