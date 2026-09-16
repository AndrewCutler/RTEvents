using System.ComponentModel.DataAnnotations;

public record UpdateEventRequestDTO(
    [Required, Range(1, int.MaxValue)] int Id,
    [MinLength(3)] string? Name,
    [MinLength(10)] string? Description,
    DateOnly? Date,
    TimeOnly? Time,
    [MinLength(3)] string? Timezone,
    [MinLength(3)] string? VenueId,
    [Range(1, int.MaxValue)] int? TicketCapacity);