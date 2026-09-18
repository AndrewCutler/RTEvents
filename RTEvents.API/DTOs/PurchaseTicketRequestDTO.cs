using System.ComponentModel.DataAnnotations;

public record PurchaseTicketRequestDTO(
    [Required, Range(1, int.MaxValue)] int Quantity,
    [Required, Range(1, int.MaxValue)] int EventId,
    [Required, MinLength(1)] string PaymentDetails
);
