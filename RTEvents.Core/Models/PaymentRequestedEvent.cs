public record PaymentRequestedEvent
{
    public int PaymentId { get; set; }
    public decimal Cost { get; set; }
    public string PaymentDetails { get; set; } = string.Empty;
}