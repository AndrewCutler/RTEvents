public record EventsReportDTO(
    decimal TotalSales,
    int TotalTicketsSold,
    int TotalTicketsHeld,
    int TotalTickesetAvailable,
    List<EventReportDTO> EventReports)
{
    public static EventsReportDTO FromDomain(EventsReport report)
    {
        return new EventsReportDTO(
            report.TotalSales,
            report.TotalTicketsSold,
            report.TotalTicketsHeld,
            report.TotalTicketsAvailable,
            report.EventReports.Select(EventReportDTO.FromDomain).ToList()
        );
    }
}

public record EventReportDTO(
    int EventId,
    string EventName,
    decimal EventSales,
    int EventTicketsSold,
    int EventTicketsHeld,
    int EventTicketsAvailable,
    int VenueId,
    string VenueName)
{
    public static EventReportDTO FromDomain(EventReport report)
    {
        return new EventReportDTO(
            report.EventId,
            report.EventName,
            report.EventSales,
            report.EventTicketsSold,
            report.EventTicketsHeld,
            report.EventTicketsAvailable,
            report.VenueId,
            report.VenueName
        );
    }
}