// All that's needed for this POC is the PaymentId and outcome.
public record PaymentResponse(int PaymentId, bool Success);