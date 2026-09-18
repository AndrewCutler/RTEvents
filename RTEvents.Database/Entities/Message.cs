// Local table to stub out actual messaging system (RabbitMQ, Azure, etc.).
public class Message
{
    public int Id { get; set; }
    // PaymentResponseEvent or PaymentRequestedEvent
    public string Payload { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public enum MessageType
{
    PaymentRequested = 0,
    PaymentSucceeded,
    PaymentFailed,
}