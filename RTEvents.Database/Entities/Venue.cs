public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Location, capacity, etc.
    public ICollection<Event> Events {get;set;} = [];
}