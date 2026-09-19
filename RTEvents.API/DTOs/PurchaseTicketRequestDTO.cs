using System.ComponentModel.DataAnnotations;

public record PurchaseTicketRequestDTO(
    [Required, Range(1, int.MaxValue)] int Quantity, // Not an ideal max; real-world restrictions would be much more modest. Must also be enforced in the service layer.
    [Required, Range(1, int.MaxValue)] int EventId,
    [Required, MinLength(1)] string PaymentDetails
);
