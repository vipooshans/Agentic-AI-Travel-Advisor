using System.Globalization;
using System.Text.RegularExpressions;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.AI.Tools;

namespace TravelAdvisor.Infrastructure.AI.Agents;

public sealed record BookingTurn(string Status, string Reply);

/// <summary>
/// Two-step booking: a request produces a proposal from a live quote; only an explicit confirmation in
/// a later turn calls the backend, and the reply reports exactly what the backend returned.
/// </summary>
public class BookingAgent
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const RegexOptions Ci = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public async Task<BookingTurn> ProposeAsync(string message, TravelPlan? plan, AgentToolContext context, CancellationToken cancellationToken)
    {
        context.UseAgent(AgentNames.Booking);

        if (plan is null)
        {
            return new BookingTurn(ChatStatus.Info,
                "I can prepare a booking once we have a plan. Ask me to plan a trip first (for example \"Plan a 3-day trip to Ella under Rs. 50,000\"), then say \"book the hotel\" or \"book the package\".");
        }

        var wantsPackage = Regex.IsMatch(message, @"\b(package|tour)\b", Ci);
        var wantsHotel = Regex.IsMatch(message, @"\b(hotel|room|stay|inn|lodge|accommodation|guest\s*house)\b", Ci);

        var namedHotel = plan.Hotels.FirstOrDefault(h => message.Contains(h.Name, StringComparison.OrdinalIgnoreCase));
        var namedPackage = plan.TravelPackages.FirstOrDefault(p => message.Contains(p.Title, StringComparison.OrdinalIgnoreCase));
        var hotel = namedHotel ?? plan.Hotels.FirstOrDefault(h => h.Selected);
        var package = namedPackage ?? plan.TravelPackages.FirstOrDefault(p => p.Selected);

        object args;
        string? note = null;
        if ((namedPackage is not null || (wantsPackage && !wantsHotel)) && package is not null)
        {
            args = new
            {
                travelPackageId = package.PackageId,
                checkIn = plan.StartDate.ToString("yyyy-MM-dd", Inv),
                guests = plan.Travelers
            };
        }
        else if (hotel is not null && !(wantsPackage && !wantsHotel && namedHotel is null))
        {
            var guests = Math.Min(plan.Travelers, hotel.Capacity > 0 ? hotel.Capacity : plan.Travelers);
            if (hotel.Rooms > 1)
                note = $"Your group needs {hotel.Rooms} rooms; this request covers one room for {guests} guest(s). Ask again after confirming to book the next room.";
            args = new
            {
                roomId = hotel.RoomId,
                checkIn = plan.StartDate.ToString("yyyy-MM-dd", Inv),
                checkOut = plan.StartDate.AddDays(hotel.Nights).ToString("yyyy-MM-dd", Inv),
                guests
            };
        }
        else
        {
            return new BookingTurn(ChatStatus.Info, wantsPackage
                ? "Your current plan doesn't include a package. Ask me to plan with a bigger budget or say \"book the hotel\"."
                : "Your current plan doesn't include a hotel to book.");
        }

        var result = await context.Registry.ExecuteAsync(ToolNames.CreateBooking, args, context, cancellationToken);
        if (!result.Success)
            return new BookingTurn(ChatStatus.BookingFailed, $"I couldn't prepare that booking: {result.Error} Nothing was booked.");

        if (result.Data is BookingToolResult { Status: "unavailable" } unavailable)
            return new BookingTurn(ChatStatus.BookingFailed, $"That option is no longer available: {unavailable.Message} Nothing was booked. Ask me to re-plan and I'll look for alternatives.");

        if (context.NewProposal is not { } proposal)
            return new BookingTurn(ChatStatus.BookingFailed, "I couldn't prepare that booking. Nothing was booked.");

        return new BookingTurn(ChatStatus.BookingProposal, DescribeProposal(proposal, note));
    }

    public async Task<BookingTurn> ConfirmAsync(BookingProposal proposal, AgentToolContext context, CancellationToken cancellationToken)
    {
        context.UseAgent(AgentNames.Booking);

        object args = proposal.Kind == "room"
            ? new
            {
                roomId = proposal.RoomId,
                checkIn = proposal.CheckIn.ToString("yyyy-MM-dd", Inv),
                checkOut = proposal.CheckOut?.ToString("yyyy-MM-dd", Inv),
                guests = proposal.Guests
            }
            : new
            {
                travelPackageId = proposal.TravelPackageId,
                checkIn = proposal.CheckIn.ToString("yyyy-MM-dd", Inv),
                guests = proposal.Guests
            };

        var result = await context.Registry.ExecuteAsync(ToolNames.CreateBooking, args, context, cancellationToken);
        if (!result.Success || context.CreatedBooking is not { } booking)
        {
            var reason = context.BookingError ?? result.Error ?? "the booking service rejected the request";
            return new BookingTurn(ChatStatus.BookingFailed, $"I couldn't submit the booking: {reason} Nothing was booked.");
        }

        var status = booking.Status.ToString().ToUpperInvariant();
        var who = proposal.Kind == "room" ? "hotel owner" : "travel agent";
        var next = booking.Status == BookingStatus.Pending
            ? $"It stays {status} until the {who} confirms it; you can follow it under My Bookings."
            : "You can follow it under My Bookings.";
        return new BookingTurn(ChatStatus.BookingCreated,
            $"Your booking request #{booking.Id} for {proposal.Title} has been submitted. The system recorded status {status}, " +
            $"total Rs. {booking.TotalPrice.ToString("N0", Inv)}. {next}");
    }

    public static string DescribeProposal(BookingProposal proposal, string? note = null)
    {
        var dates = proposal.Kind == "room" && proposal.CheckOut is { } checkOut
            ? $"{proposal.CheckIn:yyyy-MM-dd} to {checkOut:yyyy-MM-dd} ({checkOut.DayNumber - proposal.CheckIn.DayNumber} night(s))"
            : $"starting {proposal.CheckIn:yyyy-MM-dd}";
        var lines = new List<string>
        {
            "Please confirm this booking request:",
            $"  • {proposal.Title}",
            $"  • {dates}, {proposal.Guests} guest(s)",
            $"  • Price quoted by the system: Rs. {proposal.QuotedTotal.ToString("N0", Inv)}"
        };
        if (note is not null)
            lines.Add(note);
        lines.Add($"Reply \"confirm\" before {proposal.ExpiresAt:HH:mm} UTC to submit it, or \"no\" to cancel. Nothing has been booked yet; once submitted it stays PENDING until the provider confirms it.");
        return string.Join("\n", lines);
    }
}
