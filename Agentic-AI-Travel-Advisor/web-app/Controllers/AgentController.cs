using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Web.Models;
using TravelAdvisor.Web.Services;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "TRAVEL_AGENT")]
public class AgentController : Controller
{
    private readonly TravelApiClient _api;

    public AgentController(TravelApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Travel Agent Dashboard";
        ViewData["Role"] = "Travel Agent";
        ViewData["Portal"] = "Agent";

        var packages = await _api.GetMyPackagesAsync();
        var bookings = await _api.GetBookingsAsync();

        ViewBag.PackageCount = packages.Count;
        ViewBag.BookingCount = bookings.Count;
        ViewBag.DestinationCount = packages.Select(p => p.DestinationId).Distinct().Count();

        return View();
    }

    public async Task<IActionResult> Packages()
    {
        ViewData["Title"] = "My Packages";
        ViewData["Role"] = "Travel Agent";
        ViewData["Portal"] = "Agent";
        return View(await _api.GetMyPackagesAsync());
    }

    [HttpGet]
    public async Task<IActionResult> CreatePackage()
    {
        ViewData["Title"] = "Create Package";
        ViewData["Role"] = "Travel Agent";
        ViewData["Portal"] = "Agent";
        ViewBag.Destinations = await _api.GetDestinationsAsync();
        return View(new PackageFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePackage(PackageFormViewModel model)
    {
        ViewBag.Destinations = await _api.GetDestinationsAsync();
        if (!ModelState.IsValid) return View(model);

        var result = await _api.CreatePackageAsync(new CreatePackageRequest
        {
            DestinationId = model.DestinationId,
            Title = model.Title,
            Description = model.Description,
            Price = model.Price,
            DurationDays = model.DurationDays
        });

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Failed to create package.");
            return View(model);
        }

        return RedirectToAction(nameof(Packages));
    }

    [HttpGet]
    public async Task<IActionResult> EditPackage(int id)
    {
        var package = await _api.GetPackageAsync(id);
        if (package is null) return NotFound();

        ViewData["Title"] = "Edit Package";
        ViewData["Role"] = "Travel Agent";
        ViewData["Portal"] = "Agent";
        ViewBag.Destinations = await _api.GetDestinationsAsync();

        return View(new PackageFormViewModel
        {
            Id = package.Id,
            DestinationId = package.DestinationId,
            Title = package.Title,
            Description = package.Description,
            Price = package.Price,
            DurationDays = package.DurationDays
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPackage(PackageFormViewModel model)
    {
        ViewBag.Destinations = await _api.GetDestinationsAsync();
        if (!ModelState.IsValid) return View(model);

        var success = await _api.UpdatePackageAsync(model.Id, new UpdatePackageRequest
        {
            DestinationId = model.DestinationId,
            Title = model.Title,
            Description = model.Description,
            Price = model.Price,
            DurationDays = model.DurationDays
        });

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Failed to update package.");
            return View(model);
        }

        return RedirectToAction(nameof(Packages));
    }

    public async Task<IActionResult> Activities(int id)
    {
        var package = await _api.GetPackageAsync(id);
        if (package is null) return NotFound();

        ViewData["Title"] = $"Activities - {package.Title}";
        ViewData["Role"] = "Travel Agent";
        ViewData["Portal"] = "Agent";
        ViewBag.Package = package;

        return View(package);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddActivity(ActivityFormViewModel model)
    {
        await _api.AddActivityAsync(model.PackageId, new CreateActivityRequest
        {
            Title = model.Title,
            Description = model.Description,
            DayNumber = model.DayNumber,
            Price = model.Price,
            SortOrder = model.SortOrder
        });

        return RedirectToAction(nameof(Activities), new { id = model.PackageId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteActivity(int packageId, int activityId)
    {
        await _api.DeleteActivityAsync(packageId, activityId);
        return RedirectToAction(nameof(Activities), new { id = packageId });
    }

    public async Task<IActionResult> Bookings()
    {
        ViewData["Title"] = "Package Bookings";
        ViewData["Role"] = "Travel Agent";
        ViewData["Portal"] = "Agent";
        return View(await _api.GetBookingsAsync());
    }
}
