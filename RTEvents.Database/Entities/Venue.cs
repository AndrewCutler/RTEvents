public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    // Location, etc.
    public ICollection<Event> Events {get;set;} = [];
}