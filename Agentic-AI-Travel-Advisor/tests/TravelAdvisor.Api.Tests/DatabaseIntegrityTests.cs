using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;
using static TravelAdvisor.Api.Tests.ApiTestHelpers;

namespace TravelAdvisor.Api.Tests;

/// <summary>
/// Database tests against the same PostgreSQL the API uses. Constraint checks are plain SQL run inside a
/// transaction that is always rolled back, so they prove what the database itself enforces (not just EF
/// validation) and leave no rows behind. Dates are in 2031 so they never meet the API tests' bookings.
/// </summary>
[Collection("api")]
public class DatabaseIntegrityTests(ApiFixture fx, ITestOutputHelper output)
{
    private const string AdminId = """(SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail" = 'ADMIN@TRAVELADVISOR.COM')""";
    private const string RoomId = """(SELECT min("Id") FROM "Rooms")""";
    private const string PackageId = """(SELECT min("Id") FROM "TravelPackages")""";
    private const string HotelId = """(SELECT min("Id") FROM "Hotels")""";

    private static string Booking(string userId = AdminId, string roomId = RoomId, string packageId = "NULL",
        string checkIn = "2031-01-10", string checkOut = "2031-01-12", string guests = "2", string status = "0", string total = "100") =>
        $"""
        INSERT INTO "Bookings" ("UserId","RoomId","TravelPackageId","CheckIn","CheckOut","Guests","Status","TotalPrice","CreatedAt","UpdatedAt")
        VALUES ({userId}, {roomId}, {packageId}, '{checkIn}', '{checkOut}', {guests}, {status}, {total}, now(), now())
        """;

    private const string LastBooking = """(SELECT max("Id") FROM "Bookings")""";

    private static string Payment(string reference = "'DB-TEST-REF-1'", string amount = "100", string currency = "'LKR'") =>
        $"""
        INSERT INTO "Payments" ("BookingId","Amount","Currency","Method","Status","TransactionReference","CreatedAt","UpdatedAt")
        VALUES ({LastBooking}, {amount}, {currency}, 1, 0, {reference}, now(), now())
        """;

    private static string Review(string rating = "5", string hotelId = HotelId, string packageId = "NULL") =>
        $"""
        INSERT INTO "Reviews" ("UserId","BookingId","HotelId","TravelPackageId","Rating","Status","CreatedAt","UpdatedAt")
        VALUES ({AdminId}, {LastBooking}, {hotelId}, {packageId}, {rating}, 0, now(), now())
        """;

    /// <summary>(case, SQL, expected SQLSTATE, expected constraint name)</summary>
    public static TheoryData<string, string, string, string> Violations => new()
    {
        // Foreign keys (23503)
        { "room pointing at a missing hotel", """UPDATE "Rooms" SET "HotelId" = -1 WHERE "Id" = """ + RoomId, "23503", "FK_Rooms_Hotels_HotelId" },
        { "booking for a missing user", Booking(userId: "'no-such-user'"), "23503", "FK_Bookings_AspNetUsers_UserId" },
        { "booking for a missing room", Booking(roomId: "-1"), "23503", "FK_Bookings_Rooms_RoomId" },
        { "payment for a missing booking", Payment().Replace(LastBooking, "-1"), "23503", "FK_Payments_Bookings_BookingId" },
        { "delete a destination that has packages (restrict)", """DELETE FROM "Destinations" WHERE "Id" = (SELECT "DestinationId" FROM "TravelPackages" ORDER BY "Id" LIMIT 1)""", "23503", "FK_TravelPackages_Destinations_DestinationId" },
        { "delete a role that users hold (restrict)", """DELETE FROM "Roles" WHERE "Name" = 'USER'""", "23503", "FK_AspNetUsers_Roles_RoleId" },
        { "delete a room that has a booking (restrict)", Booking() + """; DELETE FROM "Rooms" WHERE "Id" = """ + RoomId, "23503", "FK_Bookings_Rooms_RoomId" },
        { "delete a booking that has a payment (restrict)", Booking() + "; " + Payment() + """; DELETE FROM "Bookings" WHERE "Id" = """ + LastBooking, "23503", "FK_Payments_Bookings_BookingId" },

        // Unique constraints (23505)
        { "two accounts with the same normalised email", """UPDATE "AspNetUsers" SET "NormalizedEmail" = 'ADMIN@TRAVELADVISOR.COM' WHERE "NormalizedEmail" = 'OWNER@TRAVELADVISOR.COM'""", "23505", "UX_AspNetUsers_NormalizedEmail" },
        { "duplicate destination name in a country", """INSERT INTO "Destinations" ("Name","Country","CreatedAt","UpdatedAt") SELECT "Name","Country",now(),now() FROM "Destinations" ORDER BY "Id" LIMIT 1""", "23505", "IX_Destinations_Name_Country" },
        { "duplicate room name in a hotel", """INSERT INTO "Rooms" ("HotelId","Name","RoomType","PricePerNight","Capacity","IsAvailable","CreatedAt","UpdatedAt") SELECT "HotelId","Name","RoomType",1,1,true,now(),now() FROM "Rooms" ORDER BY "Id" LIMIT 1""", "23505", "IX_Rooms_HotelId_Name" },
        { "duplicate role name", """INSERT INTO "Roles" ("Name") VALUES ('ADMIN')""", "23505", "IX_Roles_Name" },
        { "duplicate system setting key", """INSERT INTO "SystemSettings" ("Key","Value","CreatedAt","UpdatedAt") SELECT "Key",'x',now(),now() FROM "SystemSettings" ORDER BY "Id" LIMIT 1""", "23505", "IX_SystemSettings_Key" },
        { "duplicate payment reference", Booking() + "; " + Payment() + "; " + Payment(), "23505", "IX_Payments_TransactionReference" },
        { "two calendar overrides for one room night", """INSERT INTO "RoomAvailability" ("RoomId","Date","IsBlocked","CreatedAt","UpdatedAt") VALUES (""" + RoomId + """, '2031-02-01', true, now(), now()), (""" + RoomId + """, '2031-02-01', false, now(), now())""", "23505", "IX_RoomAvailability_RoomId_Date" },
        { "second review for the same booking", Booking() + "; " + Review() + "; " + Review(rating: "3"), "23505", "IX_Reviews_BookingId" },

        // NOT NULL (23502)
        { "hotel without a name", """UPDATE "Hotels" SET "Name" = NULL WHERE "Id" = """ + HotelId, "23502", "" },
        { "destination without a country", """UPDATE "Destinations" SET "Country" = NULL WHERE "Id" = (SELECT min("Id") FROM "Destinations")""", "23502", "" },
        { "booking without a user", Booking(userId: "NULL"), "23502", "" },
        { "payment without a currency", Booking() + "; " + Payment(currency: "NULL"), "23502", "" },

        // Check constraints (23514)
        { "booking for a room and a package", Booking(packageId: PackageId), "23514", "CK_Bookings_RoomOrPackage" },
        { "booking for neither room nor package", Booking(roomId: "NULL"), "23514", "CK_Bookings_RoomOrPackage" },
        { "check-out equal to check-in", Booking(checkOut: "2031-01-10"), "23514", "CK_Bookings_Dates" },
        { "zero guests", Booking(guests: "0"), "23514", "CK_Bookings_Guests" },
        { "negative booking total", Booking(total: "-1"), "23514", "CK_Bookings_TotalPrice" },
        { "room capacity of zero", """UPDATE "Rooms" SET "Capacity" = 0 WHERE "Id" = """ + RoomId, "23514", "CK_Rooms_Capacity" },
        { "negative room price", """UPDATE "Rooms" SET "PricePerNight" = -1 WHERE "Id" = """ + RoomId, "23514", "CK_Rooms_Price" },
        { "package lasting zero days", """UPDATE "TravelPackages" SET "DurationDays" = 0 WHERE "Id" = """ + PackageId, "23514", "CK_TravelPackages_Duration" },
        { "package for zero travelers", """UPDATE "TravelPackages" SET "MaxTravelers" = 0 WHERE "Id" = """ + PackageId, "23514", "CK_TravelPackages_MaxTravelers" },
        { "activity on day zero", """UPDATE "PackageActivities" SET "DayNumber" = 0 WHERE "Id" = (SELECT min("Id") FROM "PackageActivities")""", "23514", "CK_PackageActivities_Day" },
        { "rating of six", Booking() + "; " + Review(rating: "6"), "23514", "CK_Reviews_Rating" },
        { "rating of zero", Booking() + "; " + Review(rating: "0"), "23514", "CK_Reviews_Rating" },
        { "review targeting a hotel and a package", Booking() + "; " + Review(packageId: PackageId), "23514", "CK_Reviews_Target" },
        { "payment of zero", Booking() + "; " + Payment(amount: "0"), "23514", "CK_Payments_Amount" },
        { "negative price override", """INSERT INTO "RoomAvailability" ("RoomId","Date","IsBlocked","PriceOverride","CreatedAt","UpdatedAt") VALUES (""" + RoomId + """, '2031-02-02', false, -5, now(), now())""", "23514", "CK_RoomAvailability_Price" },
        { "budget minimum above maximum", """INSERT INTO "TravelPreferences" ("UserId","BudgetMin","BudgetMax","CreatedAt","UpdatedAt") VALUES (""" + AdminId + """, 90000, 10000, now(), now())""", "23514", "CK_TravelPreferences_Budget" },
        { "itinerary ending before it starts", """INSERT INTO "Itineraries" ("UserId","Title","StartDate","EndDate","Status","Travelers","CreatedAt","UpdatedAt") VALUES (""" + AdminId + """, 'Backwards', '2031-03-05', '2031-03-01', 0, 1, now(), now())""", "23514", "CK_Itineraries_Dates" },

        // Exclusion constraint (23P01)
        { "overlapping stays in the same room", Booking() + "; " + Booking(checkIn: "2031-01-11", checkOut: "2031-01-13"), "23P01", AppDbContext.RoomNoOverlapConstraint },
        { "same stay booked twice", Booking() + "; " + Booking(), "23P01", AppDbContext.RoomNoOverlapConstraint }
    };

    /// <summary>Statements the database must accept.</summary>
    public static TheoryData<string, string> Accepted => new()
    {
        { "back-to-back stays in the same room", Booking() + "; " + Booking(checkIn: "2031-01-12", checkOut: "2031-01-14") },
        { "overlap with a cancelled stay", Booking(status: "2") + "; " + Booking(checkIn: "2031-01-11", checkOut: "2031-01-13") },
        { "overlapping stays in different rooms", Booking() + "; " + Booking(roomId: """(SELECT max("Id") FROM "Rooms")""") },
        { "package booking without a room", Booking(roomId: "NULL", packageId: PackageId) },
        { "one review per booking", Booking() + "; " + Review() },
        { "budget minimum equal to maximum", """INSERT INTO "TravelPreferences" ("UserId","BudgetMin","BudgetMax","CreatedAt","UpdatedAt") VALUES (""" + AdminId + """, 50000, 50000, now(), now())""" },
        { "same-day itinerary", """INSERT INTO "Itineraries" ("UserId","Title","StartDate","EndDate","Status","Travelers","CreatedAt","UpdatedAt") VALUES (""" + AdminId + """, 'Day trip', '2031-03-01', '2031-03-01', 0, 1, now(), now())""" }
    };

    private async Task<PostgresException?> RunAndRollbackAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fx.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
            return null;
        }
        catch (PostgresException ex)
        {
            return ex;
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(fx.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    [SkippableTheory]
    [MemberData(nameof(Violations))]
    public async Task Database_rejects_invalid_data(string @case, string sql, string sqlState, string constraint)
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var bookingsBefore = await ScalarAsync<long>("""SELECT count(*) FROM "Bookings" WHERE "CheckIn" >= '2031-01-01'""");

        var error = await RunAndRollbackAsync(sql);

        Assert.True(error is not null, $"'{@case}' was accepted by the database");
        output.WriteLine($"{@case}: {error!.SqlState} {error.ConstraintName} - {error.MessageText}");
        Assert.Equal(sqlState, error.SqlState);
        if (constraint.Length > 0)
            Assert.Equal(constraint, error.ConstraintName);
        Assert.Equal(bookingsBefore, await ScalarAsync<long>("""SELECT count(*) FROM "Bookings" WHERE "CheckIn" >= '2031-01-01'"""));
    }

    [SkippableTheory]
    [MemberData(nameof(Accepted))]
    public async Task Database_accepts_valid_edge_cases(string @case, string sql)
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var error = await RunAndRollbackAsync(sql);
        Assert.True(error is null, $"'{@case}' was rejected: {error?.SqlState} {error?.ConstraintName} {error?.MessageText}");
    }

    [SkippableFact]
    public async Task Every_domain_table_and_named_constraint_exists()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        string[] tables =
        [
            "AspNetUsers", "Roles", "UserProfiles", "Hotels", "Rooms", "RoomAvailability", "Destinations", "TravelPackages",
            "PackageActivities", "Transportation", "Bookings", "Payments", "TravelPreferences", "Itineraries", "ItineraryItems",
            "Reviews", "AIConversations", "AIRecommendations", "SystemSettings"
        ];
        foreach (var table in tables)
            Assert.True(await ScalarAsync<bool>($"SELECT to_regclass('public.\"{table}\"') IS NOT NULL"), $"table {table} is missing");

        var constraints = new List<string>();
        await using (var connection = new NpgsqlConnection(fx.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT conname FROM pg_constraint WHERE connamespace = 'public'::regnamespace AND contype IN ('c','x')", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                constraints.Add(reader.GetString(0));
        }

        var expected = Violations.Select(row => (string)row[3]).Where(c => c.StartsWith("CK_") || c.StartsWith("EX_")).Distinct().ToList();
        expected.AddRange(["CK_Transportation_Price", "CK_Transportation_Capacity", "CK_Transportation_Duration", "CK_TravelPackages_Price", "CK_PackageActivities_Price", "CK_Itineraries_Travelers"]);
        foreach (var name in expected)
            Assert.Contains(name, constraints);
        output.WriteLine($"{constraints.Count} check/exclusion constraints in the schema: {string.Join(", ", constraints.Order())}");

        var foreignKeys = await ScalarAsync<long>("SELECT count(*) FROM pg_constraint WHERE connamespace = 'public'::regnamespace AND contype = 'f'");
        output.WriteLine($"{foreignKeys} foreign keys");
        Assert.True(foreignKeys >= 25, $"only {foreignKeys} foreign keys");
    }

    [SkippableFact]
    public async Task Delete_behaviours_cascade_or_set_null_as_configured()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        await using var connection = new NpgsqlConnection(fx.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        async Task<long> Count(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        async Task Exec(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }

        await Exec("""INSERT INTO "Destinations" ("Name","Country","CreatedAt","UpdatedAt") VALUES ('DB Test Island','Nowhere',now(),now())""");
        await Exec($"""
            INSERT INTO "Itineraries" ("UserId","Title","StartDate","EndDate","Status","Travelers","DestinationId","CreatedAt","UpdatedAt")
            VALUES ({AdminId}, 'Cascade check', '2031-04-01', '2031-04-02', 0, 1, (SELECT "Id" FROM "Destinations" WHERE "Name" = 'DB Test Island'), now(), now())
            """);
        await Exec("""
            INSERT INTO "ItineraryItems" ("ItineraryId","DayNumber","Title","SortOrder")
            SELECT "Id", d, 'Item ' || d, d FROM "Itineraries", generate_series(1, 3) AS d WHERE "Title" = 'Cascade check'
            """);

        // Itinerary -> Destination is SET NULL: deleting the destination keeps the itinerary.
        await Exec("""DELETE FROM "Destinations" WHERE "Name" = 'DB Test Island'""");
        Assert.Equal(1, await Count("""SELECT count(*) FROM "Itineraries" WHERE "Title" = 'Cascade check' AND "DestinationId" IS NULL"""));

        // ItineraryItems -> Itinerary is CASCADE.
        Assert.Equal(3, await Count("""SELECT count(*) FROM "ItineraryItems" i JOIN "Itineraries" t ON t."Id" = i."ItineraryId" WHERE t."Title" = 'Cascade check'"""));
        var itemsBefore = await Count("""SELECT count(*) FROM "ItineraryItems" """);
        await Exec("""DELETE FROM "Itineraries" WHERE "Title" = 'Cascade check'""");
        Assert.Equal(itemsBefore - 3, await Count("""SELECT count(*) FROM "ItineraryItems" """));

        // RoomAvailability -> Room is CASCADE (room without bookings).
        await Exec("""INSERT INTO "Rooms" ("HotelId","Name","RoomType","PricePerNight","Capacity","IsAvailable","CreatedAt","UpdatedAt") VALUES (""" + HotelId + """, 'DB Cascade Room', 'Single', 1000, 1, true, now(), now())""");
        await Exec("""INSERT INTO "RoomAvailability" ("RoomId","Date","IsBlocked","CreatedAt","UpdatedAt") SELECT "Id", '2031-05-01', true, now(), now() FROM "Rooms" WHERE "Name" = 'DB Cascade Room'""");
        await Exec("""DELETE FROM "Rooms" WHERE "Name" = 'DB Cascade Room'""");
        Assert.Equal(0, await Count("""SELECT count(*) FROM "RoomAvailability" WHERE "Date" = '2031-05-01'"""));

        await transaction.RollbackAsync();
    }

    [SkippableFact]
    public async Task Unit_of_work_rolls_back_everything_when_the_action_fails()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var name = $"Rollback {Guid.NewGuid():N}"[..30];

        using (var scope = fx.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync<int>(async ct =>
            {
                db.Destinations.Add(new Destination { Name = name, Country = "Rollbackistan" });
                await unitOfWork.SaveChangesAsync(ct);
                Assert.Equal(1, await db.Destinations.CountAsync(d => d.Name == name, ct));
                throw new InvalidOperationException("simulated failure after the first write");
            }));

            Assert.Empty(db.ChangeTracker.Entries());
        }

        Assert.Equal(0L, await ScalarAsync<long>($"SELECT count(*) FROM \"Destinations\" WHERE \"Name\" = '{name}'"));
    }

    [SkippableFact]
    public async Task Concurrent_edits_of_a_booking_are_detected_by_the_row_version()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (_, room) = await fx.CreateApprovedRoomWithOwnerAsync();
        var guest = fx.Authed((await fx.RegisterUserAsync()).Token);
        var checkIn = TodayUtc.AddDays(90 + Random.Shared.Next(0, 150));
        var created = await guest.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(1) });
        var bookingId = (await created.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!.Id;

        using var scopeA = fx.Factory.Services.CreateScope();
        using var scopeB = fx.Factory.Services.CreateScope();
        var a = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var b = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();
        var first = await a.Bookings.SingleAsync(x => x.Id == bookingId);
        var second = await b.Bookings.SingleAsync(x => x.Id == bookingId);

        first.Status = BookingStatus.Confirmed;
        await a.SaveChangesAsync();
        second.Status = BookingStatus.Cancelled;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => b.SaveChangesAsync());
        Assert.Equal((int)BookingStatus.Confirmed, await ScalarAsync<int>($"SELECT \"Status\" FROM \"Bookings\" WHERE \"Id\" = {bookingId}"));
    }

    [SkippableFact]
    public async Task Audit_timestamps_are_set_on_insert_and_only_updated_at_moves_on_update()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var name = $"Audit {Guid.NewGuid():N}"[..30];
        int id;
        DateTime createdAt;

        using (var scope = fx.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var destination = new Destination { Name = name, Country = "Auditland", CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            db.Destinations.Add(destination);
            var before = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
            id = destination.Id;
            createdAt = destination.CreatedAt;
            Assert.True(createdAt >= before, "CreatedAt should be stamped by the interceptor, not taken from the client");
            Assert.Equal(destination.CreatedAt, destination.UpdatedAt);
        }

        await Task.Delay(50);
        using (var scope = fx.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var destination = await db.Destinations.SingleAsync(d => d.Id == id);
            destination.Description = "Updated";
            destination.CreatedAt = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }

        using (var scope = fx.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.Destinations.AsNoTracking().SingleAsync(d => d.Id == id);
            Assert.Equal(createdAt, stored.CreatedAt, TimeSpan.FromMilliseconds(1));
            Assert.True(stored.UpdatedAt > stored.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
            db.Destinations.Remove(stored);
            await db.SaveChangesAsync();
        }
    }

    [SkippableFact]
    public async Task Seeding_twice_does_not_duplicate_reference_data()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        const string countsSql = """
            SELECT (SELECT count(*) FROM "Roles") || '/' || (SELECT count(*) FROM "Destinations") || '/' ||
                   (SELECT count(*) FROM "SystemSettings") || '/' || (SELECT count(*) FROM "AspNetUsers" WHERE "Email" LIKE '%@traveladvisor.com')
            """;
        var before = await ScalarAsync<string>(countsSql);

        await DbSeeder.SeedAsync(fx.Factory.Services, seedDemoUsers: true);

        Assert.Equal(before, await ScalarAsync<string>(countsSql));
        output.WriteLine($"roles/destinations/settings/demo users: {before}");
    }

    [SkippableFact]
    public async Task Booking_written_through_the_api_is_stored_consistently()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await fx.CreateApprovedRoomWithOwnerAsync(price: 9_000m);
        var guestAuth = await fx.RegisterUserAsync();
        var guest = fx.Authed(guestAuth.Token);
        var checkIn = TodayUtc.AddDays(100 + Random.Shared.Next(0, 150));

        var created = await guest.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(3), guests = 2 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = (await created.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;

        await using (var connection = new NpgsqlConnection(fx.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""SELECT "UserId","RoomId","TravelPackageId","CheckIn","CheckOut","Guests","Status","TotalPrice","CancelledAt" FROM "Bookings" WHERE "Id" = @id""", connection);
            command.Parameters.AddWithValue("id", booking.Id);
            await using var row = await command.ExecuteReaderAsync();
            Assert.True(await row.ReadAsync());
            Assert.Equal(guestAuth.User.Id, row.GetString(0));
            Assert.Equal(room.Id, row.GetInt32(1));
            Assert.True(row.IsDBNull(2));
            Assert.Equal(checkIn, row.GetDateTime(3).ToUniversalTime());
            Assert.Equal(checkIn.AddDays(3), row.GetDateTime(4).ToUniversalTime());
            Assert.Equal(2, row.GetInt32(5));
            Assert.Equal((int)BookingStatus.Pending, row.GetInt32(6));
            Assert.Equal(27_000m, row.GetDecimal(7));
            Assert.True(row.IsDBNull(8));
        }

        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 2 })).StatusCode);
        Assert.Equal((int)BookingStatus.Cancelled, await ScalarAsync<int>($"SELECT \"Status\" FROM \"Bookings\" WHERE \"Id\" = {booking.Id}"));
        Assert.True(await ScalarAsync<bool>($"SELECT \"CancelledAt\" IS NOT NULL FROM \"Bookings\" WHERE \"Id\" = {booking.Id}"));

        // The cancelled stay no longer blocks the room, at the API and in the exclusion constraint.
        var rebooked = await guest.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(3), guests = 2 });
        Assert.Equal(HttpStatusCode.Created, rebooked.StatusCode);
        Assert.Equal(1L, await ScalarAsync<long>($"SELECT count(*) FROM \"Bookings\" WHERE \"RoomId\" = {room.Id} AND \"Status\" <> 2"));
    }

    // ---------- Migrations (separate, freshly created database) ----------

    [SkippableFact]
    public async Task Migrations_build_the_schema_match_the_model_and_roll_back_cleanly()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var builder = new NpgsqlConnectionStringBuilder(fx.ConnectionString);
        var databaseName = $"migration_check_{Guid.NewGuid():N}"[..32];
        var adminConnection = new NpgsqlConnectionStringBuilder(fx.ConnectionString) { Database = "postgres" }.ConnectionString;
        builder.Database = databaseName;

        await using (var admin = new NpgsqlConnection(adminConnection))
        {
            await admin.OpenAsync();
            await new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin).ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString).Options;
            await using var db = new AppDbContext(options);

            var defined = db.Database.GetMigrations().ToList();
            Assert.Empty(await db.Database.GetAppliedMigrationsAsync());

            await db.Database.MigrateAsync();
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(defined, applied);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            output.WriteLine($"applied {applied.Count} migrations: {string.Join(", ", applied)}");

            Assert.Empty(ModelDifferences(db));

            // Every migration's Down() must run, leaving no domain tables behind, and Up() must work again afterwards.
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(Migration.InitialDatabase);
            Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
            await using (var check = new NpgsqlConnection(builder.ConnectionString))
            {
                await check.OpenAsync();
                var leftover = await new NpgsqlCommand("""SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'""", check).ExecuteScalarAsync();
                Assert.Equal(0L, Convert.ToInt64(leftover));
            }

            await db.Database.MigrateAsync();
            Assert.Equal(defined, (await db.Database.GetAppliedMigrationsAsync()).ToList());
            await using (var check = new NpgsqlConnection(builder.ConnectionString))
            {
                await check.OpenAsync();
                var exclusion = await new NpgsqlCommand($"SELECT count(*) FROM pg_constraint WHERE conname = '{AppDbContext.RoomNoOverlapConstraint}'", check).ExecuteScalarAsync();
                Assert.Equal(1L, Convert.ToInt64(exclusion));
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin).ExecuteNonQueryAsync();
        }
    }

#pragma warning disable EF1001 // Internal EF Core API: the same comparison 'dotnet ef migrations add' performs.
    private static IReadOnlyList<MigrationOperation> ModelDifferences(AppDbContext db)
    {
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot?.Model
                       ?? throw new InvalidOperationException("No model snapshot found.");
        if (snapshot is IMutableModel mutable)
            snapshot = mutable.FinalizeModel();
        snapshot = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot);

        var designModel = db.GetService<IDesignTimeModel>().Model;
        return db.GetService<IMigrationsModelDiffer>().GetDifferences(snapshot.GetRelationalModel(), designModel.GetRelationalModel());
    }
#pragma warning restore EF1001
}
