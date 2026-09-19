using RTEvents.Tests.Support;

namespace RTEvents.Tests.Core;

public class ReportingServiceTests
{
    [Fact]
    public async Task Empty_report_has_zero_totals()
    {
        using var db = new MockDatabase();
        var report = await new ReportingService(db.Context.Object).GenerateReportByEventAsync();
        Assert.Empty(report.EventReports);
        Assert.Equal(0m, report.TotalSales);
        Assert.Equal(0, report.TotalTicketsAvailable);
        Assert.Equal(0, report.TotalTicketsHeld);
        Assert.Equal(0, report.TotalTicketsSold);
        db.VerifySaved(0);
    }

    [Fact]
    public async Task Report_aggregates_each_event_and_totals()
    {
        using var db = new MockDatabase();
        var first = Samples.Event(1);
        var tickets = first.HoldTickets(2).ToList();
        Samples.Set(tickets[0], nameof(Ticket.Cost), 12.50m);
        Samples.Set(tickets[1], nameof(Ticket.Cost), 7.25m);
        tickets[0].MarkSold();
        var second = Samples.Event(2, 20);
        second.Update("Second event", null, new DateOnly(2026, 11, 1), null, null, null, null);
        Samples.Set(second.HoldTickets(1).Single(), nameof(Ticket.Cost), 3m);
        db.Context.Object.Events = MockDatabase.Set(first, second).Object;

        var report = await new ReportingService(db.Context.Object).GenerateReportByEventAsync();

        Assert.Equal(new[] { 2, 1 }, report.EventReports.Select(e => e.EventId));
        Assert.Equal(22.75m, report.TotalSales);
        Assert.Equal(27, report.TotalTicketsAvailable);
        Assert.Equal(2, report.TotalTicketsHeld);
        Assert.Equal(1, report.TotalTicketsSold);
        var item = report.EventReports[1];
        Assert.Equal("Concert", item.EventName);
        Assert.Equal(19.75m, item.EventSales);
        Assert.Equal(8, item.EventTicketsAvailable);
        Assert.Equal(1, item.EventTicketsSold);
        Assert.Equal(1, item.EventTicketsHeld);
        Assert.Equal(2, item.VenueId);
        Assert.Equal("The hall", item.VenueName);
        db.VerifySaved(0);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 1, 0)]
    [InlineData(0, 0, 0)]
    public async Task Report_paginates_after_sorting_and_totals_only_the_page(int skip, int take, int count)
    {
        using var db = new MockDatabase();
        var older = Samples.Event(1, 10);
        var newer = Samples.Event(2, 20);
        newer.Update(null, null, new DateOnly(2027, 1, 1), null, null, null, null);
        db.Context.Object.Events = MockDatabase.Set(older, newer).Object;
        var report = await new ReportingService(db.Context.Object).GenerateReportByEventAsync(skip, take);
        Assert.Equal(count, report.EventReports.Count);
        Assert.Equal(count * 10, report.TotalTicketsAvailable);
        if (count > 0) Assert.Equal(1, report.EventReports[0].EventId);
    }
}
