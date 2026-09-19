using System.Net;

namespace RTEvents.Tests.Core;

public class ExceptionTests
{
    public static TheoryData<DomainException, HttpStatusCode, string> Errors => new()
    {
        { new DomainException("custom", HttpStatusCode.Forbidden), HttpStatusCode.Forbidden, "custom" },
        { new EventNotFoundException(7), HttpStatusCode.NotFound, "Event with id 7 not found" },
        { new VenueNotFoundException(2), HttpStatusCode.NotFound, "Venue with id 2 not found" },
        { new PurchaseNotFoundException(4), HttpStatusCode.NotFound, "Purchase with id 4 not found." },
        { new EventOverCapacityException(99), HttpStatusCode.UnprocessableEntity, "Target capacity of 99 exceeds venue limits." },
        { new MissingIdempotencyKeyException("Purchase"), HttpStatusCode.BadRequest, "An idempotency key must be provided for Purchase." },
        { new MismatchedIdempotencyKeyException("old", "new"), HttpStatusCode.Conflict, "Idempotency key mismatch: expected old but received new." }
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void Domain_errors_preserve_status_and_context(DomainException error, HttpStatusCode code, string message)
    {
        Assert.Equal(code, error.StatusCode);
        Assert.Equal(message, error.Message);
    }
}
