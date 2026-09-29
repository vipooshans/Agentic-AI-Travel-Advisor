using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TravelAdvisor.Core.Entities;

namespace TravelAdvisor.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> AppRoles => Set<Role>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<TravelPackage> TravelPackages => Set<TravelPackage>();
    public DbSet<PackageActivity> PackageActivities => Set<PackageActivity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TravelPreferences> TravelPreferences => Set<TravelPreferences>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<AIConversation> AIConversations => Set<AIConversation>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Npgsql only accepts UTC for timestamptz; clients post dates like "2026-10-05" with no offset.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Utc ? v
                    : v.Kind == DateTimeKind.Local ? v.ToUniversalTime()
                    : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasIndex(r => r.Name).IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TravelPreferences>(entity =>
        {
            entity.HasOne(tp => tp.User)
                .WithOne(u => u.TravelPreferences)
                .HasForeignKey<TravelPreferences>(tp => tp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Bookings_RoomOrPackage",
                "(\"RoomId\" IS NOT NULL AND \"TravelPackageId\" IS NULL) OR (\"RoomId\" IS NULL AND \"TravelPackageId\" IS NOT NULL)"));
        });

        builder.Entity<Itinerary>(entity =>
        {
            entity.HasOne(i => i.Destination)
                .WithMany(d => d.Itineraries)
                .HasForeignKey(i => i.DestinationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ItineraryItem>(entity =>
        {
            entity.HasOne(i => i.Itinerary)
                .WithMany(it => it.Items)
                .HasForeignKey(i => i.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PackageActivity>(entity =>
        {
            entity.HasOne(a => a.TravelPackage)
                .WithMany(p => p.Activities)
                .HasForeignKey(a => a.TravelPackageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
