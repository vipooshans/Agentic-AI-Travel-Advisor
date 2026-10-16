using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Web.Models;
using TravelAdvisor.Web.Services;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "USER")]
public class CustomerController : Controller
{
    private readonly TravelApiClient _api;

    public CustomerController(TravelApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Traveler Dashboard";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var bookings = await _api.GetBookingsAsync();

        ViewBag.TotalBookings = bookings.Count;
        ViewBag.PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending);
        ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed);
        ViewBag.CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
        ViewBag.TotalSpent = bookings
            .Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Completed)
            .Sum(b => b.TotalPrice);

        if (TempData["PromptResponseJson"] is string json)
        {
            try
            {
                ViewBag.PromptResponse = System.Text.Json.JsonSerializer.Deserialize<BookingAgentPromptResponse>(json);
            }
            catch { }
        }

        try
        {
            ViewBag.Insights = await _api.GetBookingInsightsAsync();
        }
        catch
        {
            ViewBag.Insights = new List<BookingInsightDto>();
        }

        try
        {
            var historyResponse = await _api.GetBookingAgentHistoryAsync();
            ViewBag.ConversationHistory = historyResponse?.History ?? [];
            ViewBag.ConversationId = historyResponse?.ConversationId ?? 0;
        }
        catch
        {
            ViewBag.ConversationHistory = new List<BookingAgentMessageDto>();
            ViewBag.ConversationId = 0;
        }

        var recentBookings = bookings.OrderByDescending(b => b.CreatedAt).Take(5).ToList();
        return View(recentBookings);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> QuickPromptJson([FromBody] BookingAgentPromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
            return BadRequest(new { message = "Prompt cannot be empty" });

        request.PreviousPrompts = (request.PreviousPrompts ?? [])
            .Where(prompt => !string.IsNullOrWhiteSpace(prompt))
            .TakeLast(10)
            .ToList();
        var response = await _api.ProcessBookingPromptAsync(request);
        if (response == null)
        {
            return Json(new BookingAgentPromptResponse
            {
                ConversationId = request.ConversationId ?? 0,
                ActionType = "chat",
                Message = "The AI service is temporarily unavailable. Please try again in a few moments."
            });
        }
        return Json(response);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            TempData["Error"] = "Please enter a booking or cancellation prompt.";
            return RedirectToAction(nameof(Dashboard));
        }

        var response = await _api.ProcessBookingPromptAsync(prompt);
        if (response != null)
        {
            TempData["PromptResponseJson"] = System.Text.Json.JsonSerializer.Serialize(response);
        }
        else
        {
            TempData["Error"] = "The AI booking assistant could not process your request at this time.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public IActionResult ConfirmProposal()
    {
        TempData["Error"] = "Open your AI Concierge conversation and choose Review & Reserve on a current proposal.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmProposal(BookingProposalReviewViewModel request)
    {
        if (string.IsNullOrWhiteSpace(request.ProposalToken))
        {
            TempData["Error"] = "That proposal is missing or expired. Ask the AI Concierge for a fresh quote.";
            return RedirectToAction(nameof(Dashboard));
        }

        await PopulateCustomerDetailsAsync(request);
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProposal(BookingProposalReviewViewModel request)
    {
        var booking = await _api.ConfirmBookingProposalAsync(request);
        if (booking != null)
        {
            TempData["Success"] = $"Your reservation #{booking.Id} has been successfully submitted! It is currently Pending confirmation.";
            return RedirectToAction(nameof(Bookings));
        }

        request.ErrorMessage = "The reservation could not be submitted. The quote may have expired or availability may have changed. Return to the AI Concierge for an updated quote.";
        await PopulateCustomerDetailsAsync(request);
        return View("ConfirmProposal", request);
    }

    private async Task PopulateCustomerDetailsAsync(BookingProposalReviewViewModel model)
    {
        model.CustomerName = User.Identity?.Name ?? string.Empty;
        model.CustomerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var profile = await _api.GetMyProfileAsync();
        model.PhoneNumber = profile?.PhoneNumber;
    }

    public async Task<IActionResult> Bookings(string? status)
    {
        ViewData["Title"] = "My Bookings";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var bookings = await _api.GetBookingsAsync();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var filterStatus))
        {
            bookings = bookings.Where(b => b.Status == filterStatus).ToList();
            ViewBag.ActiveFilter = filterStatus.ToString();
        }
        else
        {
            ViewBag.ActiveFilter = "All";
        }

        return View(bookings.OrderByDescending(b => b.CreatedAt).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var (succeeded, error) = await _api.CancelBookingAsync(id);
        if (succeeded)
        {
            TempData["Success"] = $"Booking #{id} has been cancelled.";
        }
        else
        {
            TempData["Error"] = error ?? $"Unable to cancel booking #{id}.";
        }

        return RedirectToAction(nameof(Bookings));
    }

    public async Task<IActionResult> Hotels(string? q)
    {
        ViewData["Title"] = "Hotels & Stays";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var hotels = await _api.GetHotelsAsync(ApprovalStatus.Approved);
        if (!string.IsNullOrWhiteSpace(q))
        {
            hotels = hotels.Where(h =>
                (h.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || (h.City ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || (h.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.SearchQuery = q;
        return View(hotels);
    }

    public async Task<IActionResult> Hotel(int id)
    {
        ViewData["Title"] = "Hotel Details";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var hotel = await _api.GetHotelAsync(id);
        if (hotel is null) return NotFound();

        ViewBag.Rooms = await _api.GetRoomsAsync(id);
        return View(hotel);
    }

    [HttpGet]
    public async Task<IActionResult> BookHotel(int hotelId, int roomId)
    {
        ViewData["Title"] = "Reserve Room";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var hotel = await _api.GetHotelAsync(hotelId);
        var rooms = await _api.GetRoomsAsync(hotelId);
        var room = rooms.FirstOrDefault(r => r.Id == roomId);

        if (hotel is null || room is null) return NotFound();

        ViewBag.Hotel = hotel;
        ViewBag.Room = room;

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        return View(new CreateBookingRequest
        {
            RoomId = roomId,
            CheckIn = tomorrow,
            CheckOut = tomorrow.AddDays(2),
            Guests = 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookHotel(int hotelId, CreateBookingRequest request)
    {
        ViewData["Title"] = "Reserve Room";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        if (!request.CheckOut.HasValue || request.CheckOut.Value <= request.CheckIn)
        {
            ModelState.AddModelError("CheckOut", "Check-out date must be after check-in date.");
        }

        if (request.Guests < 1)
        {
            ModelState.AddModelError("Guests", "At least 1 guest is required.");
        }

        if (!ModelState.IsValid)
        {
            var hotel = await _api.GetHotelAsync(hotelId);
            var rooms = await _api.GetRoomsAsync(hotelId);
            ViewBag.Hotel = hotel;
            ViewBag.Room = rooms.FirstOrDefault(r => r.Id == request.RoomId);
            return View(request);
        }

        var booking = await _api.CreateBookingAsync(request);
        if (booking is null)
        {
            ModelState.AddModelError(string.Empty, "Unable to place booking. The room may be unavailable for the selected dates.");
            var hotel = await _api.GetHotelAsync(hotelId);
            var rooms = await _api.GetRoomsAsync(hotelId);
            ViewBag.Hotel = hotel;
            ViewBag.Room = rooms.FirstOrDefault(r => r.Id == request.RoomId);
            return View(request);
        }

        TempData["Success"] = $"Booking #{booking.Id} submitted successfully! Your booking is currently Pending provider confirmation.";
        return RedirectToAction(nameof(Bookings));
    }

    public async Task<IActionResult> Packages(string? q)
    {
        ViewData["Title"] = "Tour Packages";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var packages = await _api.GetPackagesAsync(ApprovalStatus.Approved);
        if (!string.IsNullOrWhiteSpace(q))
        {
            packages = packages.Where(p =>
                (p.Title ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || (p.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.SearchQuery = q;
        return View(packages);
    }

    public async Task<IActionResult> Package(int id)
    {
        ViewData["Title"] = "Package Details";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var package = await _api.GetPackageAsync(id);
        if (package is null) return NotFound();

        return View(package);
    }

    [HttpGet]
    public async Task<IActionResult> BookPackage(int packageId)
    {
        ViewData["Title"] = "Book Tour Package";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var package = await _api.GetPackageAsync(packageId);
        if (package is null) return NotFound();

        ViewBag.Package = package;

        var start = DateTime.UtcNow.Date.AddDays(7);
        return View(new CreateBookingRequest
        {
            TravelPackageId = packageId,
            CheckIn = start,
            Guests = 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookPackage(CreateBookingRequest request)
    {
        ViewData["Title"] = "Book Tour Package";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        if (request.Guests < 1)
        {
            ModelState.AddModelError("Guests", "At least 1 traveler is required.");
        }

        if (!ModelState.IsValid)
        {
            if (request.TravelPackageId.HasValue)
                ViewBag.Package = await _api.GetPackageAsync(request.TravelPackageId.Value);
            return View(request);
        }

        var booking = await _api.CreateBookingAsync(request);
        if (booking is null)
        {
            ModelState.AddModelError(string.Empty, "Unable to complete package booking. Please verify the date and guest capacity.");
            if (request.TravelPackageId.HasValue)
                ViewBag.Package = await _api.GetPackageAsync(request.TravelPackageId.Value);
            return View(request);
        }

        TempData["Success"] = $"Package booking #{booking.Id} submitted successfully! Your booking is currently Pending agent confirmation.";
        return RedirectToAction(nameof(Bookings));
    }

    public async Task<IActionResult> Destinations()
    {
        ViewData["Title"] = "Destinations";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        return View(await _api.GetDestinationsAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        ViewData["Title"] = "My Profile";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        var profile = await _api.GetMyProfileAsync() ?? new UserProfileDto();
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileDto model)
    {
        ViewData["Title"] = "My Profile";
        ViewData["Role"] = "Traveler";
        ViewData["Portal"] = "Customer";

        if (!ModelState.IsValid) return View(model);

        var ok = await _api.UpdateMyProfileAsync(model);
        if (ok)
        {
            TempData["Success"] = "Your profile has been updated.";
        }
        else
        {
            TempData["Error"] = "Unable to update profile.";
        }

        return View(model);
    }
}
