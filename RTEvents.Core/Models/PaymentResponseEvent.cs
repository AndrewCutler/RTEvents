// All that's needed for this POC is the PaymentId and outcome.
public record PaymentResponseEvent(int PaymentId, bool Success);