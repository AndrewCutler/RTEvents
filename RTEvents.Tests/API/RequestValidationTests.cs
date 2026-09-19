using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RTEvents.Tests.API;

public class RequestValidationTests
{
    // MVC reads validation metadata from positional record constructor parameters.
    public static TheoryData<Type, string, object?> InvalidValues => new()
    {
        { typeof(CreateEventRequestDTO), "Name", null },
        { typeof(CreateEventRequestDTO), "Name", "ab" },
        { typeof(CreateEventRequestDTO), "Description", null },
        { typeof(CreateEventRequestDTO), "Description", "123456789" },
        { typeof(CreateEventRequestDTO), "Timezone", null },
        { typeof(CreateEventRequestDTO), "Timezone", "UT" },
        { typeof(CreateEventRequestDTO), "VenueId", 0 },
        { typeof(CreateEventRequestDTO), "VenueId", -1 },
        { typeof(CreateEventRequestDTO), "TicketCapacity", 0 },
        { typeof(CreateEventRequestDTO), "TicketCapacity", -1 },
        { typeof(UpdateEventRequestDTO), "Id", 0 },
        { typeof(UpdateEventRequestDTO), "Name", "ab" },
        { typeof(UpdateEventRequestDTO), "Description", "123456789" },
        { typeof(UpdateEventRequestDTO), "Timezone", "UT" },
        { typeof(UpdateEventRequestDTO), "VenueId", "12" },
        { typeof(UpdateEventRequestDTO), "TicketCapacity", 0 },
        { typeof(PurchaseTicketRequestDTO), "Quantity", 0 },
        { typeof(PurchaseTicketRequestDTO), "Quantity", -1 },
        { typeof(PurchaseTicketRequestDTO), "EventId", 0 },
        { typeof(PurchaseTicketRequestDTO), "PaymentDetails", null },
        { typeof(PurchaseTicketRequestDTO), "PaymentDetails", "" },
        { typeof(CreatePaymentResponseRequestDTO), "PaymentId", 0 },
        { typeof(CreatePaymentResponseRequestDTO), "PaymentId", -1 }
    };

    public static TheoryData<Type, string, object?> ValidValues => new()
    {
        { typeof(CreateEventRequestDTO), "Name", "abc" },
        { typeof(CreateEventRequestDTO), "Description", "1234567890" },
        { typeof(CreateEventRequestDTO), "Timezone", "UTC" },
        { typeof(CreateEventRequestDTO), "VenueId", 1 },
        { typeof(CreateEventRequestDTO), "TicketCapacity", 1 },
        { typeof(CreateEventRequestDTO), "TicketCapacity", int.MaxValue },
        { typeof(UpdateEventRequestDTO), "Id", 1 },
        { typeof(UpdateEventRequestDTO), "Name", null },
        { typeof(UpdateEventRequestDTO), "Description", null },
        { typeof(UpdateEventRequestDTO), "Timezone", null },
        { typeof(UpdateEventRequestDTO), "VenueId", null },
        { typeof(UpdateEventRequestDTO), "TicketCapacity", null },
        { typeof(UpdateEventRequestDTO), "TicketCapacity", 1 },
        { typeof(PurchaseTicketRequestDTO), "Quantity", 1 },
        { typeof(PurchaseTicketRequestDTO), "EventId", 1 },
        { typeof(PurchaseTicketRequestDTO), "PaymentDetails", "x" },
        { typeof(CreatePaymentResponseRequestDTO), "PaymentId", 1 }
    };

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Request_validation_rejects_invalid_field(Type type, string parameter, object? value)
    {
        Assert.Contains(Attributes(type, parameter), attribute => !attribute.IsValid(value));
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void Request_validation_accepts_boundaries_and_optional_nulls(Type type, string parameter, object? value)
    {
        var attributes = Attributes(type, parameter).ToList();
        Assert.NotEmpty(attributes);
        Assert.All(attributes, attribute => Assert.True(attribute.IsValid(value)));
    }

    private static IEnumerable<ValidationAttribute> Attributes(Type type, string parameter) =>
        type.GetConstructors().Single().GetParameters().Single(p => p.Name == parameter).GetCustomAttributes<ValidationAttribute>();
}
