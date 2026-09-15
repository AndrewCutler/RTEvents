using Microsoft.EntityFrameworkCore;

public class RTEventsDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<PricingTier> PricingTiers { get; set; }
    public DbSet<Venue> Venues { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Event
        builder.Entity<Event>()
            .HasMany(e => e.Tickets)
            .WithOne()
            .HasForeignKey(e => e.EventId);

        builder.Entity<Event>()
            .HasMany(e => e.PricingTiers);

        builder.Entity<Event>()
            .HasOne(e => e.Venue)
            .WithMany()
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
            .WithMany()
            .HasForeignKey(e => e.EventId);

        builder.Entity<Ticket>()
            .ToTable(t => t.HasCheckConstraint("CK_Event_TIcketCost", "[Cost] >= 0"));

        // Venue
        builder.Entity<Venue>()
            .HasMany(e => e.Events)
            .WithOne()
            .HasForeignKey(e => e.VenueId);
    }
}