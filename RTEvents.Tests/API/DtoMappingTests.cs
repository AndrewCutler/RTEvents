using RTEvents.API.DTOs;
using RTEvents.Tests.Support;

namespace RTEvents.Tests.API;

public class DtoMappingTests
{
    [Fact]
    public void Event_mapping_copies_all_exposed_fields()
    {
        var dto = EventDTO.FromDomain(Samples.Event());
        Assert.Equal(new EventDTO(7, "Concert", "An evening concert", new(2026, 10, 1), new(19, 30), "UTC", 10), dto);
    }

    [Fact]
    public void Availability_mapping_preserves_distinct_counts()
    {
        Assert.Equal(new TicketAvailabilityDTO(7, 11, 3, 5, 19), TicketAvailabilityDTO.FromDomain(new(7, 11, 3, 5, 19)));
    }

    [Theory]
    [InlineData(PurchaseStatus.Pending)]
    [InlineData(PurchaseStatus.Failed)]
    [InlineData(PurchaseStatus.Succeeded)]
    public void Purchase_mapping_copies_total_and_status(PurchaseStatus status)
    {
        var purchase = Samples.Set(new Purchase(123.45m, []), nameof(Purchase.Status), status);
        Assert.Equal(new PurchaseTicketsResponseDTO(123.45m, status), PurchaseTicketsResponseDTO.FromDomain(purchase));
    }

    [Fact]
    public void Report_mapping_copies_totals_and_nested_event_fields()
    {
        var item = new EventReport { EventId = 7, EventName = "Concert", EventSales = 123.45m, EventTicketsSold = 3, EventTicketsHeld = 2, EventTicketsAvailable = 5, VenueId = 9, VenueName = "Hall" };
        var report = new EventsReport { TotalSales = 456.78m, TotalTicketsSold = 13, TotalTicketsHeld = 12, TotalTicketsAvailable = 15, EventReports = [item] };
        var dto = EventsReportDTO.FromDomain(report);
        Assert.Equal(456.78m, dto.TotalSales);
        Assert.Equal(13, dto.TotalTicketsSold);
        Assert.Equal(12, dto.TotalTicketsHeld);
        Assert.Equal(15, dto.TotalTickesetAvailable);
        var mapped = Assert.Single(dto.EventReports);
        Assert.Equal(7, mapped.EventId);
        Assert.Equal("Concert", mapped.EventName);
        Assert.Equal(123.45m, mapped.EventSales);
        Assert.Equal(3, mapped.EventTicketsSold);
        Assert.Equal(2, mapped.EventTicketsHeld);
        Assert.Equal(5, mapped.EventTicketsAvailable);
        Assert.Equal(9, mapped.VenueId);
        Assert.Equal("Hall", mapped.VenueName);
        report.EventReports.Clear();
        Assert.Single(dto.EventReports);
    }

    [Fact]
    public void Empty_report_maps_to_empty_collection()
    {
        Assert.Empty(EventsReportDTO.FromDomain(new EventsReport()).EventReports);
    }
}
