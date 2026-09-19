using System.Net;

public class MismatchedIdempotencyKeyException : DomainException
{
    public MismatchedIdempotencyKeyException(string expected, string actual) :
        base($"Idempotency key mismatch: expected {expected} but received {actual}.", HttpStatusCode.Conflict) { }
}