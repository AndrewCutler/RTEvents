using System.ComponentModel.DataAnnotations;

public record CreatePaymentResponseRequestDTO(
    [Range(1, int.MaxValue)] int PaymentId,
    bool Success);
