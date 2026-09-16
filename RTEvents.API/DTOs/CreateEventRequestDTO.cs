using System.ComponentModel.DataAnnotations;

public record CreateEventRequestDTO(
    [Required, MinLength(3)] string Name,
    [Required, MinLength(10)] string Description,
    [Required] DateOnly Date,
    [Required] TimeOnly Time,
    [Required, MinLength(3)] string Timezone,
    [Required, Range(1, int.MaxValue)] int VenueId,
    [Required, Range(1, int.MaxValue)] int TicketCapacity);