using Microsoft.EntityFrameworkCore;

public class ReportingService : IReportingService
{
    private readonly RTEventsDbContext _context;

    public ReportingService(RTEventsDbContext context)
    {
        _context = context;
    }

    public async Task<EventsReport> GenerateReportByEventAsync(int skip = 0, int take = 100)
    {
        var result = new EventsReport();

        var events = await _context.Events
            .OrderByDescending(e => e.Date)
            .Skip(skip)
            .Take(take)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.AvailableTicketCount,
                Venue = new
                {
                    e.Venue.Id,
                    e.Venue.Name,
                },
                Tickets = e.Tickets.Select(t => new
                {
                    t.Cost,
                    t.Id,
                    t.AvailabilityStatus,
                }).ToList(),
            })
            .ToListAsync();

        var totalOfAllEventTickets = 0.0m;
        var totalSoldCount = 0;
        var totalAvailableCount = 0;
        var totalHeldCount = 0;

        foreach (var @event in events)
        {
            var totalCostOfEventTickets = 0.0m;
            var eventSoldCount = 0;
            var eventHeldCount = 0;

            foreach (var ticket in @event.Tickets)
            {
                totalCostOfEventTickets += ticket.Cost;

                switch (ticket.AvailabilityStatus)
                {
                    case AvailabilityStatus.Held:
                        eventHeldCount++;
                        break;
                    case AvailabilityStatus.Sold:
                        eventSoldCount++;
                        break;
                    default: break;
                }
            }

            totalOfAllEventTickets += totalCostOfEventTickets;
            totalSoldCount += eventSoldCount;
            totalAvailableCount += @event.AvailableTicketCount;
            totalHeldCount += eventHeldCount;

            var report = new EventReport
            {
                EventId = @event.Id,
                EventName = @event.Name,
                EventTicketsAvailable = @event.AvailableTicketCount,
                EventSales = totalCostOfEventTickets,
                EventTicketsHeld = eventHeldCount,
                EventTicketsSold = eventSoldCount,
                VenueId = @event.Venue.Id,
                VenueName = @event.Venue.Name,
            };

            result.TotalSales = totalOfAllEventTickets;
            result.TotalTicketsSold = totalSoldCount;
            result.TotalTicketsHeld = totalHeldCount;
            result.TotalTicketsAvailable = totalAvailableCount;
            result.EventReports.Add(report);
        }

        return result;
    }
}