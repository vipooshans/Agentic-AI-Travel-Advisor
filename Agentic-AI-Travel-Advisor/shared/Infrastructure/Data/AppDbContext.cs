using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TravelAdvisor.Core.Entities;

namespace TravelAdvisor.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public const string RoomNoOverlapConstraint = "EX_Bookings_Room_NoOverlap";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> AppRoles => Set<Role>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomAvailability> RoomAvailability => Set<RoomAvailability>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<TravelPackage> TravelPackages => Set<TravelPackage>();
    public DbSet<PackageActivity> PackageActivities => Set<PackageActivity>();
    public DbSet<Transportation> Transportation => Set<Transportation>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<TravelPreferences> TravelPreferences => Set<TravelPreferences>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<AIConversation> AIConversations => Set<AIConversation>();
    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Npgsql only accepts UTC for timestamptz; clients post dates like "2026-10-05" with no offset.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
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

        builder.HasPostgresExtension("btree_gist");

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(r => r.Name).HasMaxLength(32);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("UX_AspNetUsers_NormalizedEmail");
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.Property(p => p.PhoneNumber).HasMaxLength(32);
            entity.Property(p => p.Nationality).HasMaxLength(64);
            entity.Property(p => p.AvatarUrl).HasMaxLength(500);
            entity.Property(p => p.Bio).HasMaxLength(1000);
            entity.Property(p => p.PreferredCurrency).HasMaxLength(3).IsRequired();
            entity.HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TravelPreferences>(entity =>
        {
            entity.Property(p => p.PreferredClimate).HasMaxLength(64);
            entity.Property(p => p.Interests).HasMaxLength(500);
            entity.Property(p => p.AccommodationPreference).HasMaxLength(32);
            entity.Property(p => p.TransportPreference).HasMaxLength(32);
            entity.HasOne(tp => tp.User)
                .WithOne(u => u.TravelPreferences)
                .HasForeignKey<TravelPreferences>(tp => tp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_TravelPreferences_Budget",
                "\"BudgetMin\" IS NULL OR \"BudgetMax\" IS NULL OR \"BudgetMin\" <= \"BudgetMax\""));
        });

        builder.Entity<Destination>(entity =>
        {
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
            entity.Property(d => d.Country).HasMaxLength(100).IsRequired();
            entity.Property(d => d.Description).HasMaxLength(2000);
            entity.Property(d => d.ImageUrl).HasMaxLength(500);
            entity.HasIndex(d => new { d.Name, d.Country }).IsUnique();
        });

        builder.Entity<Hotel>(entity =>
        {
            entity.Property(h => h.Name).HasMaxLength(150).IsRequired();
            entity.Property(h => h.Address).HasMaxLength(250).IsRequired();
            entity.Property(h => h.City).HasMaxLength(100).IsRequired();
            entity.Property(h => h.Country).HasMaxLength(100).IsRequired();
            entity.Property(h => h.Description).HasMaxLength(2000);
            entity.Property(h => h.ImageUrl).HasMaxLength(500);
            entity.HasIndex(h => h.City);
            entity.HasIndex(h => h.ApprovalStatus);
        });

        builder.Entity<Room>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(100).IsRequired();
            entity.Property(r => r.RoomType).HasMaxLength(50).IsRequired();
            entity.HasIndex(r => new { r.HotelId, r.Name }).IsUnique();
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rooms_Price", "\"PricePerNight\" >= 0");
                t.HasCheckConstraint("CK_Rooms_Capacity", "\"Capacity\" > 0");
            });
        });

        builder.Entity<RoomAvailability>(entity =>
        {
            entity.HasIndex(a => new { a.RoomId, a.Date }).IsUnique();
            entity.Property(a => a.Note).HasMaxLength(250);
            entity.HasOne(a => a.Room)
                .WithMany(r => r.Availability)
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_RoomAvailability_Price",
                "\"PriceOverride\" IS NULL OR \"PriceOverride\" >= 0"));
        });

        builder.Entity<TravelPackage>(entity =>
        {
            entity.Property(p => p.Title).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.MaxTravelers).HasDefaultValue(10);
            entity.HasIndex(p => new { p.DestinationId, p.ApprovalStatus });
            entity.HasOne(p => p.Destination)
                .WithMany(d => d.TravelPackages)
                .HasForeignKey(p => p.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TravelPackages_Price", "\"Price\" >= 0");
                t.HasCheckConstraint("CK_TravelPackages_Duration", "\"DurationDays\" > 0");
                t.HasCheckConstraint("CK_TravelPackages_MaxTravelers", "\"MaxTravelers\" > 0");
            });
        });

        builder.Entity<PackageActivity>(entity =>
        {
            entity.Property(a => a.Title).HasMaxLength(150).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(1000);
            entity.Property(a => a.Category).HasMaxLength(50);
            entity.HasOne(a => a.TravelPackage)
                .WithMany(p => p.Activities)
                .HasForeignKey(a => a.TravelPackageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PackageActivities_Price", "\"Price\" >= 0");
                t.HasCheckConstraint("CK_PackageActivities_Day", "\"DayNumber\" > 0");
            });
        });

        builder.Entity<Transportation>(entity =>
        {
            entity.Property(t => t.FromLocation).HasMaxLength(150).IsRequired();
            entity.Property(t => t.ToLocation).HasMaxLength(150).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(1000);
            entity.HasIndex(t => t.DestinationId);
            entity.HasOne(t => t.Provider)
                .WithMany()
                .HasForeignKey(t => t.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.TravelPackage)
                .WithMany(p => p.Transportation)
                .HasForeignKey(t => t.TravelPackageId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Destination)
                .WithMany(d => d.Transportation)
                .HasForeignKey(t => t.DestinationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Transportation_Price", "\"PricePerPerson\" >= 0");
                t.HasCheckConstraint("CK_Transportation_Capacity", "\"Capacity\" > 0");
                t.HasCheckConstraint("CK_Transportation_Duration", "\"DurationMinutes\" > 0");
            });
        });

        builder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.Notes).HasMaxLength(1000);
            entity.Property(b => b.Version).IsRowVersion();
            entity.HasIndex(b => new { b.RoomId, b.CheckIn, b.CheckOut });
            entity.HasIndex(b => new { b.TravelPackageId, b.CheckIn });
            entity.HasIndex(b => new { b.UserId, b.CreatedAt });
            entity.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.TravelPackage)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.TravelPackageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Bookings_RoomOrPackage",
                    "(\"RoomId\" IS NOT NULL AND \"TravelPackageId\" IS NULL) OR (\"RoomId\" IS NULL AND \"TravelPackageId\" IS NOT NULL)");
                t.HasCheckConstraint("CK_Bookings_Dates", "\"CheckOut\" > \"CheckIn\"");
                t.HasCheckConstraint("CK_Bookings_Guests", "\"Guests\" > 0");
                t.HasCheckConstraint("CK_Bookings_TotalPrice", "\"TotalPrice\" >= 0");
            });
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            entity.Property(p => p.TransactionReference).HasMaxLength(64).IsRequired();
            entity.HasIndex(p => p.TransactionReference).IsUnique();
            entity.HasIndex(p => p.BookingId);
            entity.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" > 0"));
        });

        builder.Entity<Review>(entity =>
        {
            entity.Property(r => r.Comment).HasMaxLength(2000);
            entity.HasIndex(r => r.BookingId).IsUnique();
            entity.HasIndex(r => r.HotelId);
            entity.HasIndex(r => r.TravelPackageId);
            entity.HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Hotel)
                .WithMany(h => h.Reviews)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.TravelPackage)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.TravelPackageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Reviews_Target",
                    "(\"HotelId\" IS NOT NULL AND \"TravelPackageId\" IS NULL) OR (\"HotelId\" IS NULL AND \"TravelPackageId\" IS NOT NULL)");
            });
        });

        builder.Entity<Itinerary>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Summary).HasMaxLength(4000);
            entity.HasIndex(i => i.UserId);
            entity.HasOne(i => i.Destination)
                .WithMany(d => d.Itineraries)
                .HasForeignKey(i => i.DestinationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(i => i.Conversation)
                .WithMany()
                .HasForeignKey(i => i.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Itineraries_Dates", "\"EndDate\" >= \"StartDate\"");
                t.HasCheckConstraint("CK_Itineraries_Travelers", "\"Travelers\" > 0");
            });
        });

        builder.Entity<ItineraryItem>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.Property(i => i.ItemType).HasMaxLength(32);
            entity.HasOne(i => i.Itinerary)
                .WithMany(it => it.Items)
                .HasForeignKey(i => i.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AIConversation>(entity =>
        {
            entity.Property(c => c.Title).HasMaxLength(200);
            entity.Property(c => c.Version).IsRowVersion();
            entity.HasIndex(c => new { c.UserId, c.UpdatedAt });
        });

        builder.Entity<AIRecommendation>(entity =>
        {
            entity.Property(r => r.Title).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Reason).HasMaxLength(1000);
            entity.HasIndex(r => r.ConversationId);
            entity.HasOne(r => r.Conversation)
                .WithMany(c => c.Recommendations)
                .HasForeignKey(r => r.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SystemSetting>(entity =>
        {
            entity.Property(s => s.Key).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Value).HasMaxLength(1000).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.HasIndex(s => s.Key).IsUnique();
        });
    }
}
