public class EventOverCapacityException : DomainException
{
    public EventOverCapacityException(int capacity) : base($"Target capacity of {capacity} exceeds venue limits.", System.Net.HttpStatusCode.UnprocessableEntity)
    { }
}