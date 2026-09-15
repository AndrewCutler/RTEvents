using Microsoft.EntityFrameworkCore;

public class RTEventsDbContext : DbContext
{
    public RTEventsDbContext(DbContextOptions<RTEventsDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<PricingTier> PricingTiers { get; set; }
    public DbSet<Venue> Venues { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Event
        builder.Entity<Event>()
            .HasMany(e => e.PricingTiers);

        builder.Entity<Event>()
            .HasOne(e => e.Venue)
            .WithMany(v => v.Events)
            .HasForeignKey(e => e.VenueId);

        builder.Entity<Event>()
            .Property(e => e.Name)
            .HasColumnType("nvarchar(200)");

        builder.Entity<Event>()
            .Property(e => e.Description)
            .HasColumnType("nvarchar(2000)");

        builder.Entity<Event>()
            .ToTable(t => t.HasCheckConstraint("CK_Event_TIcketCapacity", "[TicketCapacity] > 0"));

        // Ticket
        builder.Entity<Ticket>()
            .HasOne(e => e.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.EventId);

        builder.Entity<Ticket>()
            .ToTable(t => t.HasCheckConstraint("CK_Event_TIcketCost", "[Cost] >= 0"));

        // Venue
        builder.Entity<Venue>()
            .Property(e => e.Name)
            .HasColumnType("nvarchar(200)");
    }
}
