public class OutboxMessage
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public OutboxMessageStatus Status { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
}

public enum OutboxMessageStatus
{
    Pending = 0,
    Processed,
    DeadLetter,
}