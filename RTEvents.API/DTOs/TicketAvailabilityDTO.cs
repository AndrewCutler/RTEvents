public record TicketAvailabilityDTO(int EventId, int AvailableCount, int HeldCount, int SoldCount, int TicketCapacity)
{
    public static TicketAvailabilityDTO FromDomain(TicketAvailability ticketAvailability)
    {
        return new TicketAvailabilityDTO(
            ticketAvailability.EventId,
            ticketAvailability.AvailableCount,
            ticketAvailability.HeldCount,
            ticketAvailability.SoldCount,
            ticketAvailability.TicketCapacity);
    }
}