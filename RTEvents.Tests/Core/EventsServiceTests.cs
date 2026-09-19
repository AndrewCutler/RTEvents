using RTEvents.Tests.Support;

namespace RTEvents.Tests.Core;

public class EventsServiceTests
{
    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    public async Task Create_validates_capacity_adds_event_and_saves(int capacity)
    {
        using var db = new MockDatabase();
        db.Context.Object.Venues = MockDatabase.Set(new Venue { Id = 2, Capacity = 10 }).Object;
        var events = Mock.Get(db.Context.Object.Events);
        var result = await new EventsService(db.Context.Object).CreateAsync("Concert", "Description", new(2026, 10, 1), new(19, 30), "UTC", capacity, 2);
        Assert.Equal("Concert", result.Name);
        Assert.Equal("Description", result.Description);
        Assert.Equal(new DateOnly(2026, 10, 1), result.Date);
        Assert.Equal(new TimeOnly(19, 30), result.Time);
        Assert.Equal("UTC", result.Timezone);
        Assert.Equal(capacity, result.TicketCapacity);
        Assert.Equal(2, result.VenueId);
        events.Verify(s => s.Add(result), Times.Once);
        db.VerifySaved();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_rejects_missing_venue_or_excess_capacity_without_saving(bool venueExists)
    {
        using var db = new MockDatabase();
        if (venueExists) db.Context.Object.Venues = MockDatabase.Set(new Venue { Id = 2, Capacity = 5 }).Object;
        var error = await Record.ExceptionAsync(() => new EventsService(db.Context.Object).CreateAsync("Concert", "Description", default, default, "UTC", 10, 2));
        Assert.IsType(venueExists ? typeof(EventOverCapacityException) : typeof(VenueNotFoundException), error);
        Mock.Get(db.Context.Object.Events).Verify(s => s.Add(It.IsAny<Event>()), Times.Never);
        db.VerifySaved(0);
    }

    [Fact]
    public async Task Get_returns_matching_event_or_null_without_saving()
    {
        using var db = new MockDatabase();
        var e = Samples.Event();
        db.Context.Object.Events = MockDatabase.Set(e).Object;
        var service = new EventsService(db.Context.Object);
        Assert.Same(e, await service.GetByIdAsync(7));
        Assert.Null(await service.GetByIdAsync(99));
        db.VerifySaved(0);
    }

    [Fact]
    public async Task Update_changes_existing_event_and_saves()
    {
        using var db = new MockDatabase();
        var e = Samples.Event();
        db.Context.Object.Events = MockDatabase.Set(e).Object;
        db.Context.Object.Venues = MockDatabase.Set(new Venue { Id = 3, Capacity = 20 }).Object;
        var result = await new EventsService(db.Context.Object).UpdateAsync(7, "Updated", "Updated description", new(2027, 1, 2), new(20, 0), "EST", 3, 20);
        Assert.Same(e, result);
        Assert.Equal("Updated", e.Name);
        Assert.Equal("Updated description", e.Description);
        Assert.Equal(new DateOnly(2027, 1, 2), e.Date);
        Assert.Equal(new TimeOnly(20, 0), e.Time);
        Assert.Equal("EST", e.Timezone);
        Assert.Equal(3, e.VenueId);
        Assert.Equal(20, e.TicketCapacity);
        db.VerifySaved();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_rejects_invalid_venue_or_capacity_without_saving(bool venueExists)
    {
        using var db = new MockDatabase();
        db.Context.Object.Events = MockDatabase.Set(Samples.Event()).Object;
        if (venueExists) db.Context.Object.Venues = MockDatabase.Set(new Venue { Id = 2, Capacity = 5 }).Object;
        var error = await Record.ExceptionAsync(() => new EventsService(db.Context.Object).UpdateAsync(7));
        Assert.IsType(venueExists ? typeof(EventOverCapacityException) : typeof(VenueNotFoundException), error);
        db.VerifySaved(0);
    }

    [Fact]
    public async Task Delete_marks_existing_event_deleted_and_saves()
    {
        using var db = new MockDatabase();
        var e = Samples.Event();
        db.Context.Object.Events = MockDatabase.Set(e).Object;
        await new EventsService(db.Context.Object).DeleteAsync(7);
        Assert.Equal(EventStatus.Deleted, e.Status);
        db.VerifySaved();
    }

    [Fact]
    public async Task Update_and_delete_missing_event_throw_without_saving()
    {
        using var db = new MockDatabase();
        var service = new EventsService(db.Context.Object);
        await Assert.ThrowsAsync<EventNotFoundException>(() => service.UpdateAsync(99));
        await Assert.ThrowsAsync<EventNotFoundException>(() => service.DeleteAsync(99));
        db.VerifySaved(0);
    }
}
