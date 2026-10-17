using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Infrastructure.AI;
using Xunit;

namespace TravelAdvisor.UnitTests.AI;

public class ConversationalFallbackTests
{
    private static readonly DateTime Today = new(2026, 10, 17);

    [Fact]
    public void Greeting_returns_warm_booking_prompt_and_four_chips()
    {
        var result = ConversationalFallback.Decide([User("hi")], Today);

        Assert.Equal(FallbackStep.Greeting, result.Step);
        Assert.Contains("book hotels and tour packages", result.Message);
        Assert.Equal(4, result.Suggestions.Count);
        Assert.Contains(result.Suggestions, suggestion => suggestion.Label == "Hotel in Galle");
        Assert.Contains(result.Suggestions, suggestion => suggestion.Label == "Suggest something");
    }

    [Fact]
    public void Suggestion_request_returns_popular_destinations()
    {
        var result = ConversationalFallback.Decide([User("hello suggest me something")], Today);

        Assert.Equal(FallbackStep.Suggest, result.Step);
        Assert.Contains("Galle", result.Message);
        Assert.Contains("Ella", result.Message);
        Assert.Contains("Kandy", result.Message);
        Assert.Contains("Mirissa", result.Message);
    }

    [Fact]
    public void Remembers_dates_destination_and_guest_count_across_turns()
    {
        var history = new List<BookingAgentMessageDto> { User("hotel") };
        var first = ConversationalFallback.Decide(history, Today);
        Assert.Equal(FallbackStep.AskLocation, first.Step);
        history.Add(Assistant(first.Message));

        history.Add(User("this month 20 and 21"));
        var dates = ConversationalFallback.Decide(history, Today);
        Assert.Equal(FallbackStep.AskLocation, dates.Step);
        Assert.Equal(new DateTime(2026, 10, 20), dates.Slots.CheckIn);
        Assert.Equal(new DateTime(2026, 10, 21), dates.Slots.CheckOut);
        history.Add(Assistant(dates.Message));

        history.Add(User("galle"));
        var destination = ConversationalFallback.Decide(history, Today);
        Assert.Equal(FallbackStep.AskGuests, destination.Step);
        Assert.Equal("Galle", destination.Slots.Location);
        history.Add(Assistant(destination.Message));

        history.Add(User("2"));
        var ready = ConversationalFallback.Decide(history, Today);
        Assert.Equal(FallbackStep.Ready, ready.Step);
        Assert.Equal(2, ready.Slots.Guests);
        Assert.Equal(new DateTime(2026, 10, 20), ready.Slots.CheckIn);
        Assert.Equal(new DateTime(2026, 10, 21), ready.Slots.CheckOut);
    }

    [Fact]
    public void Booking_word_asks_only_for_destination_first()
    {
        var result = ConversationalFallback.Decide([User("hotel")], Today);

        Assert.Equal(FallbackStep.AskLocation, result.Step);
        Assert.Contains("Where", result.Message);
        Assert.DoesNotContain("check-out", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guests", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Explicit_iso_range_and_guest_unit_are_parsed_without_confusing_day_numbers_for_guests()
    {
        var result = ConversationalFallback.Decide(
        [
            User("I want a hotel in Ella from 2026-11-20 to 2026-11-22 for 2 guests")
        ], Today);

        Assert.Equal(FallbackStep.Ready, result.Step);
        Assert.Equal("Ella", result.Slots.Location);
        Assert.Equal(new DateTime(2026, 11, 20), result.Slots.CheckIn);
        Assert.Equal(new DateTime(2026, 11, 22), result.Slots.CheckOut);
        Assert.Equal(2, result.Slots.Guests);
    }

    [Fact]
    public void Month_name_range_is_parsed()
    {
        var result = ConversationalFallback.Decide(
        [
            User("hotel in Galle from November 20 to November 22 for 2 guests")
        ], Today);

        Assert.Equal(FallbackStep.Ready, result.Step);
        Assert.Equal(new DateTime(2026, 11, 20), result.Slots.CheckIn);
        Assert.Equal(new DateTime(2026, 11, 22), result.Slots.CheckOut);
    }

    [Fact]
    public void Past_day_without_month_rolls_forward_but_past_iso_date_is_rejected()
    {
        var dayOnly = ConversationalFallback.Decide(
        [
            User("hotel"),
            Assistant("Where in Sri Lanka would you like to stay?"),
            User("10th")
        ], Today);
        Assert.Equal(new DateTime(2026, 11, 10), dayOnly.Slots.CheckIn);

        var past = ConversationalFallback.Decide(
        [
            User("Book a hotel in Galle from 2026-10-10 to 2026-10-12 for 2 guests")
        ], Today);
        Assert.Equal(FallbackStep.AskDates, past.Step);
        Assert.Contains("already passed", past.Message);
    }

    [Fact]
    public void Confirmation_after_proposal_only_points_to_review_button()
    {
        var result = ConversationalFallback.Decide(
        [
            User("book a room"),
            Assistant("Here is your quote.", proposal: new BookingProposalDto()),
            User("yes")
        ], Today);

        Assert.Equal(FallbackStep.ConfirmHint, result.Step);
        Assert.Contains("Review & Reserve", result.Message);
        Assert.DoesNotContain("booking is confirmed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starts_collecting_new_slots_after_a_proposal()
    {
        var result = ConversationalFallback.Decide(
        [
            User("hotel in Galle"),
            Assistant("Your quote is ready.", proposal: new BookingProposalDto()),
            User("hotel")
        ], Today);

        Assert.Equal(FallbackStep.AskLocation, result.Step);
        Assert.Null(result.Slots.Location);
    }

    [Fact]
    public void Destination_matching_uses_word_boundaries()
    {
        var result = ConversationalFallback.Decide([User("I want a hotel in Galleon")], Today);

        Assert.Equal(FallbackStep.AskLocation, result.Step);
        Assert.Null(result.Slots.Location);
    }

    private static BookingAgentMessageDto User(string content) => new() { Role = "user", Content = content };

    private static BookingAgentMessageDto Assistant(string content, BookingProposalDto? proposal = null) =>
        new() { Role = "assistant", Content = content, Proposal = proposal };
}
