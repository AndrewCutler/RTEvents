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
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Payment> Payments { get; set; }

    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Message> Messages { get; set; }

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
            .Property(e => e.RowVersion)
            .IsRowVersion();

        builder.Entity<Event>()
            .ToTable(t => t.HasCheckConstraint("CK_Event_TIcketCapacity", "[TicketCapacity] > 0"));

        // Ticket
        builder.Entity<Ticket>()
            .HasOne(e => e.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.EventId);

        builder.Entity<Ticket>()
            .HasOne(e => e.Purchase)
            .WithMany(e => e.Tickets)
            .HasForeignKey(e => e.PurchaseId);

        builder.Entity<Ticket>()
            .ToTable(t => t.HasCheckConstraint("CK_Event_TIcketCost", "[Cost] >= 0"));

        builder.Entity<Ticket>()
            .Property(e => e.Cost)
            .HasPrecision(18, 2);

        // Venue
        builder.Entity<Venue>()
            .Property(e => e.Name)
            .HasColumnType("nvarchar(200)");

        // Payment
        builder.Entity<Payment>()
            .HasOne(e => e.Purchase)
            .WithOne(e => e.Payment)
            .HasForeignKey<Payment>(e => e.PurchaseId);

        builder.Entity<Payment>()
            .Property(e => e.Cost)
            .HasPrecision(18, 2);

        // Purchase
        builder.Entity<Purchase>()
            .Property(e => e.Total)
            .HasPrecision(18, 2);

        // IdempotencyKey 
        builder.Entity<IdempotencyKey>()
            .HasKey(e => e.Key);

        SeedVenues(builder);
    }

    private void SeedVenues(ModelBuilder builder)
    {
        var venues = new List<Venue>
        {
            new Venue{ Id = 1, Name = "Venue 1", Capacity = 400, },
            new Venue{ Id = 2, Name = "Venue 2", Capacity = 5000, },
            new Venue{ Id = 3, Name = "Venue 3", Capacity = 75000, },
            new Venue{ Id = 4, Name = "Venue 4", Capacity = 1000, },
            new Venue{ Id = 5, Name = "Venue 5", Capacity = 1500, },
            new Venue{ Id = 6, Name = "Venue 6", Capacity = 3, },
        };

        builder.Entity<Venue>().HasData(venues);
    }
}
