using System.ComponentModel.DataAnnotations;

public record PurchaseTicketRequestDTO(
    [Required, Range(1, int.MaxValue)] int quantity,
    [Required, Range(1, int.MaxValue)] int eventId
);
