using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Web.Models;
using TravelAdvisor.Web.Services;

namespace TravelAdvisor.Web.Controllers;

public class HomeController : Controller
{
    private readonly TravelApiClient _api;

    public HomeController(TravelApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, string? city, decimal? maxPrice, string? tab)
    {
        var destinations = await _api.GetDestinationsAsync();
        var hotels = await _api.GetHotelsAsync(ApprovalStatus.Approved);
        var packages = await _api.GetPackagesAsync(ApprovalStatus.Approved);

        if (!string.IsNullOrWhiteSpace(q))
        {
            hotels = hotels.Where(h => (h.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                                    || (h.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                                    || (h.City ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            packages = packages.Where(p => (p.Title ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                                        || (p.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            hotels = hotels.Where(h => (h.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (maxPrice.HasValue)
        {
            packages = packages.Where(p => p.Price <= maxPrice.Value).ToList();
            hotels = hotels.Where(h => h.MinPricePerNight <= maxPrice.Value).ToList();
        }

        ViewBag.Destinations = destinations;
        ViewBag.Hotels = hotels;
        ViewBag.Packages = packages;
        ViewBag.ActiveTab = string.Equals(tab, "packages", StringComparison.OrdinalIgnoreCase) ? "packages" : "hotels";
        ViewBag.Query = q;
        ViewBag.City = city;
        ViewBag.MaxPrice = maxPrice;

        return View();
    }

    public async Task<IActionResult> Hotel(int id)
    {
        var hotel = await _api.GetHotelAsync(id);
        if (hotel is null) return NotFound();
        ViewBag.Rooms = await _api.GetRoomsAsync(id);
        return View(hotel);
    }

    public async Task<IActionResult> Package(int id)
    {
        var package = await _api.GetPackageAsync(id);
        if (package is null) return NotFound();
        return View(package);
    }

    public async Task<IActionResult> Destinations()
    {
        return View(await _api.GetDestinationsAsync());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
