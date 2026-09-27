using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.Enums;
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
