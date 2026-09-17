public record EventsReport
{
    public decimal TotalSales { get; set; }
    public int TotalTicketsSold { get; set; }
    public int TotalTicketsHeld { get; set; }
    public int TotalTicketsAvailable { get; set; }
    public List<EventReport> EventReports { get; set; } = [];
}

public record EventReport
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public decimal EventSales { get; set; }
    public int EventTicketsSold { get; set; }
    public int EventTicketsHeld { get; set; }
    public int EventTicketsAvailable { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
}