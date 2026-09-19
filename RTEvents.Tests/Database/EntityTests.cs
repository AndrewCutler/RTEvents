using RTEvents.Tests.Support;

namespace RTEvents.Tests.Database;

public class EntityTests
{
    [Fact]
    public void Event_constructor_initializes_details_and_availability()
    {
        var e = Samples.Event();
        Assert.Equal("Concert", e.Name);
        Assert.Equal("An evening concert", e.Description);
        Assert.Equal(new DateOnly(2026, 10, 1), e.Date);
        Assert.Equal(new TimeOnly(19, 30), e.Time);
        Assert.Equal("UTC", e.Timezone);
        Assert.Equal(2, e.VenueId);
        Assert.Equal(10, e.TicketCapacity);
        Assert.Equal(10, e.AvailableTicketCount);
        Assert.Equal(EventStatus.Active, e.Status);
        Assert.Empty(e.Tickets);
        Assert.Empty(e.PricingTiers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void Event_constructor_rejects_missing_text(string? value)
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new Event(value!, "description", default, default, "UTC", 1, 1)).ParamName);
        Assert.Equal("description", Assert.Throws<ArgumentException>(() => new Event("name", value!, default, default, "UTC", 1, 1)).ParamName);
        Assert.Equal("timezone", Assert.Throws<ArgumentException>(() => new Event("name", "description", default, default, value!, 1, 1)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Event_constructor_and_update_reject_nonpositive_capacity_and_venue(int value)
    {
        Assert.Equal("ticketCapacity", Assert.Throws<ArgumentOutOfRangeException>(() => new Event("name", "description", default, default, "UTC", value, 1)).ParamName);
        Assert.Equal("venueId", Assert.Throws<ArgumentOutOfRangeException>(() => new Event("name", "description", default, default, "UTC", 1, value)).ParamName);
        var e = Samples.Event();
        Assert.Equal("ticketCapacity", Assert.Throws<ArgumentOutOfRangeException>(() => e.Update(null, null, null, null, null, null, value)).ParamName);
        Assert.Equal("venueId", Assert.Throws<ArgumentOutOfRangeException>(() => e.Update(null, null, null, null, null, value, null)).ParamName);
    }

    [Fact]
    public void Update_applies_supplied_fields()
    {
        var e = Samples.Event();
        e.Update("New name", "New description", new DateOnly(2027, 2, 3), new TimeOnly(12, 15), "America/New_York", 3, 20);
        Assert.Equal("New name", e.Name);
        Assert.Equal("New description", e.Description);
        Assert.Equal(new DateOnly(2027, 2, 3), e.Date);
        Assert.Equal(new TimeOnly(12, 15), e.Time);
        Assert.Equal("America/New_York", e.Timezone);
        Assert.Equal(3, e.VenueId);
        Assert.Equal(20, e.TicketCapacity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_preserves_omitted_fields_and_ignores_blank_text(string? text)
    {
        var e = Samples.Event();
        e.Update(text, text, null, null, text, null, null);
        Assert.Equal("Concert", e.Name);
        Assert.Equal("An evening concert", e.Description);
        Assert.Equal("UTC", e.Timezone);
        Assert.Equal(new DateOnly(2026, 10, 1), e.Date);
        Assert.Equal(new TimeOnly(19, 30), e.Time);
        Assert.Equal(2, e.VenueId);
        Assert.Equal(10, e.TicketCapacity);
    }

    [Fact]
    public void Event_lifecycle_methods_set_status()
    {
        var e = Samples.Event();
        e.Deactivate();
        Assert.Equal(EventStatus.Inactive, e.Status);
        e.Activate();
        Assert.Equal(EventStatus.Active, e.Status);
        e.Delete();
        Assert.Equal(EventStatus.Deleted, e.Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void HoldTickets_creates_held_tickets_and_reduces_availability(int quantity)
    {
        var e = Samples.Event();
        var tickets = e.HoldTickets(quantity).ToList();
        Assert.Equal(quantity, tickets.Count);
        Assert.Equal(tickets, e.Tickets);
        Assert.Equal(10 - quantity, e.AvailableTicketCount);
        Assert.All(tickets, t => { Assert.Equal(e.Id, t.EventId); Assert.Equal(AvailabilityStatus.Held, t.AvailabilityStatus); });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void HoldTickets_rejects_invalid_quantities_without_mutation(int quantity)
    {
        var e = Samples.Event();
        Assert.Throws<Exception>(() => e.HoldTickets(quantity));
        Assert.Empty(e.Tickets);
        Assert.Equal(10, e.AvailableTicketCount);
    }

    [Fact]
    public void HoldTickets_uses_remaining_capacity_on_subsequent_calls()
    {
        var e = Samples.Event();
        e.HoldTickets(8);
        Assert.Throws<Exception>(() => e.HoldTickets(3));
        Assert.Equal(8, e.Tickets.Count);
        Assert.Equal(2, e.AvailableTicketCount);
    }

    [Fact]
    public void Ticket_methods_change_availability()
    {
        var ticket = new Ticket(23);
        Assert.Equal(23, ticket.EventId);
        ticket.MarkSold();
        Assert.Equal(AvailabilityStatus.Sold, ticket.AvailabilityStatus);
        ticket.MarkHeld();
        Assert.Equal(AvailabilityStatus.Held, ticket.AvailabilityStatus);
    }

    [Fact]
    public void Purchase_copies_tickets_and_initializes_pending_details()
    {
        var before = DateTimeOffset.UtcNow;
        var tickets = new List<Ticket> { new(7) };
        var purchase = new Purchase(12.34m, tickets);
        Assert.Equal(12.34m, purchase.Total);
        Assert.Equal(PurchaseStatus.Pending, purchase.Status);
        Assert.InRange(purchase.CreatedAt, before, DateTimeOffset.UtcNow);
        Assert.Same(tickets[0], Assert.Single(purchase.Tickets));
        tickets.Clear();
        Assert.Single(purchase.Tickets);
    }

    [Fact]
    public void Payment_initializes_pending_details_and_supports_success_and_failure()
    {
        var purchase = new Purchase(12.34m, []);
        var before = DateTimeOffset.UtcNow;
        var payment = new Payment(12.34m, "payment token", purchase);
        Assert.Equal(12.34m, payment.Cost);
        Assert.Equal("payment token", payment.PaymentDetails);
        Assert.Same(purchase, payment.Purchase);
        Assert.InRange(payment.CreatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        payment.MarkSucceeded();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        payment.MarkFailed();
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }
}
