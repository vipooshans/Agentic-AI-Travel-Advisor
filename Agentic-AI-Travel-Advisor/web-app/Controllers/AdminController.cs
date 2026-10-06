using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Web.Models;
using TravelAdvisor.Web.Services;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminController : Controller
{
    private readonly TravelApiClient _api;

    public AdminController(TravelApiClient api)
    {
        _api = api;
    }

    private void SetAdminView(string title)
    {
        ViewData["Title"] = title;
        ViewData["Role"] = "Administrator";
        ViewData["Portal"] = "Admin";
        ViewData["StatusController"] = "Admin";
    }

    public async Task<IActionResult> Dashboard()
    {
        SetAdminView("Admin Dashboard");
        return View(await _api.GetReportSummaryAsync());
    }

    public async Task<IActionResult> Users(string? role)
    {
        SetAdminView("Users");
        ViewBag.RoleFilter = role;
        return View(await _api.GetUsersAsync(role));
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        SetAdminView("Create owner or agent");
        return View(new CreateStaffUserRequest { Role = RoleNames.HotelOwner });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateStaffUserRequest request)
    {
        SetAdminView("Create owner or agent");
        if (!ModelState.IsValid)
            return View(request);

        var created = await _api.CreateStaffUserAsync(request);
        if (created is null)
        {
            ModelState.AddModelError(string.Empty, "Could not create user. Email may already exist or the role is invalid.");
            return View(request);
        }

        TempData["Success"] = $"Created {created.Email} ({created.Role}).";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserActive(string id, bool isActive)
    {
        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentId)
        {
            TempData["Error"] = "You cannot change your own active status.";
            return RedirectToAction(nameof(Users));
        }

        if (!await _api.SetUserActiveAsync(id, isActive))
            TempData["Error"] = "Could not update user.";
        else
            TempData["Success"] = isActive ? "User activated." : "User deactivated.";

        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Destinations()
    {
        SetAdminView("Destinations");
        return View(await _api.GetDestinationsAsync());
    }

    [HttpGet]
    public IActionResult CreateDestination()
    {
        SetAdminView("Add destination");
        return View(new DestinationFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDestination(DestinationFormViewModel model)
    {
        SetAdminView("Add destination");
        if (!ModelState.IsValid || !TryNormalizeImage(model))
            return View(model);

        var created = await _api.CreateDestinationAsync(ToRequest(model));
        if (created is null)
        {
            ModelState.AddModelError(string.Empty, "Could not create destination.");
            return View(model);
        }

        TempData["Success"] = $"Added {created.Name}.";
        return RedirectToAction(nameof(Destinations));
    }

    [HttpGet]
    public async Task<IActionResult> EditDestination(int id)
    {
        var destination = await _api.GetDestinationAsync(id);
        if (destination is null)
            return NotFound();

        SetAdminView("Edit destination");
        return View(new DestinationFormViewModel
        {
            Id = destination.Id,
            Name = destination.Name,
            Country = destination.Country,
            Description = destination.Description,
            ImageUrl = destination.ImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDestination(DestinationFormViewModel model)
    {
        SetAdminView("Edit destination");
        if (!ModelState.IsValid || !TryNormalizeImage(model))
            return View(model);

        if (!await _api.UpdateDestinationAsync(model.Id, ToRequest(model)))
        {
            ModelState.AddModelError(string.Empty, "Could not update destination.");
            return View(model);
        }

        TempData["Success"] = $"Updated {model.Name}. The mobile app will show this on the next refresh.";
        return RedirectToAction(nameof(Destinations));
    }

    private bool TryNormalizeImage(DestinationFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            model.ImageUrl = null;
            return true;
        }

        model.ImageUrl = model.ImageUrl.Trim();
        if (Uri.TryCreate(model.ImageUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            return true;

        ModelState.AddModelError(nameof(model.ImageUrl), "Image URL must be an http or https address.");
        return false;
    }

    private static SaveDestinationRequest ToRequest(DestinationFormViewModel model) => new()
    {
        Name = model.Name,
        Country = model.Country,
        Description = model.Description,
        ImageUrl = model.ImageUrl
    };

    public async Task<IActionResult> Hotels(ApprovalStatus? approvalStatus)
    {
        SetAdminView("Hotels");
        ViewBag.ApprovalFilter = approvalStatus;
        return View(await _api.GetHotelsAsync(approvalStatus));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHotelApproval(int id, ApprovalStatus status)
    {
        if (!await _api.SetHotelApprovalAsync(id, status))
            TempData["Error"] = "Could not update hotel approval.";
        else
            TempData["Success"] = $"Hotel marked {status}.";
        return RedirectToAction(nameof(Hotels));
    }

    public async Task<IActionResult> Packages(ApprovalStatus? approvalStatus)
    {
        SetAdminView("Packages");
        ViewBag.ApprovalFilter = approvalStatus;
        return View(await _api.GetPackagesAsync(approvalStatus));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPackageApproval(int id, ApprovalStatus status)
    {
        if (!await _api.SetPackageApprovalAsync(id, status))
            TempData["Error"] = "Could not update package approval.";
        else
            TempData["Success"] = $"Package marked {status}.";
        return RedirectToAction(nameof(Packages));
    }

    public async Task<IActionResult> Bookings()
    {
        SetAdminView("Bookings");
        return View(await _api.GetBookingsAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBookingStatus(int id, BookingStatus status)
    {
        if (!await _api.UpdateBookingStatusAsync(id, status))
            TempData["Error"] = "Could not update booking status.";
        return RedirectToAction(nameof(Bookings));
    }
}
