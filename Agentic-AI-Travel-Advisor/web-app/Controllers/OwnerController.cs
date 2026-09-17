using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Web.Models;
using TravelAdvisor.Web.Services;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "HOTEL_OWNER")]
public class OwnerController : Controller
{
    private readonly TravelApiClient _api;

    public OwnerController(TravelApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Hotel Owner Dashboard";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";

        var hotels = await _api.GetMyHotelsAsync();
        var bookings = await _api.GetBookingsAsync();

        ViewBag.HotelCount = hotels.Count;
        ViewBag.RoomCount = hotels.Sum(h => h.RoomCount);
        ViewBag.BookingCount = bookings.Count;

        return View();
    }

    public async Task<IActionResult> Hotels()
    {
        ViewData["Title"] = "My Hotels";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";
        return View(await _api.GetMyHotelsAsync());
    }

    [HttpGet]
    public IActionResult CreateHotel()
    {
        ViewData["Title"] = "Add Hotel";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";
        return View(new HotelFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHotel(HotelFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.CreateHotelAsync(new CreateHotelRequest
        {
            Name = model.Name,
            Address = model.Address,
            City = model.City,
            Country = model.Country,
            Description = model.Description
        });

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Failed to create hotel.");
            return View(model);
        }

        return RedirectToAction(nameof(Hotels));
    }

    [HttpGet]
    public async Task<IActionResult> EditHotel(int id)
    {
        var hotel = await _api.GetHotelAsync(id);
        if (hotel is null) return NotFound();

        ViewData["Title"] = "Edit Hotel";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";

        return View(new HotelFormViewModel
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            Description = hotel.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHotel(HotelFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var success = await _api.UpdateHotelAsync(model.Id, new UpdateHotelRequest
        {
            Name = model.Name,
            Address = model.Address,
            City = model.City,
            Country = model.Country,
            Description = model.Description
        });

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Failed to update hotel.");
            return View(model);
        }

        return RedirectToAction(nameof(Hotels));
    }

    public async Task<IActionResult> Rooms(int id)
    {
        var hotel = await _api.GetHotelAsync(id);
        if (hotel is null) return NotFound();

        ViewData["Title"] = $"Rooms - {hotel.Name}";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";
        ViewBag.Hotel = hotel;

        return View(await _api.GetRoomsAsync(id));
    }

    [HttpGet]
    public IActionResult AddRoom(int hotelId)
    {
        ViewData["Title"] = "Add Room";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";
        return View(new RoomFormViewModel { HotelId = hotelId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRoom(RoomFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.CreateRoomAsync(model.HotelId, new CreateRoomRequest
        {
            Name = model.Name,
            RoomType = model.RoomType,
            PricePerNight = model.PricePerNight,
            Capacity = model.Capacity
        });

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Failed to add room.");
            return View(model);
        }

        return RedirectToAction(nameof(Rooms), new { id = model.HotelId });
    }

    [HttpGet]
    public async Task<IActionResult> EditRoom(int hotelId, int roomId)
    {
        var rooms = await _api.GetRoomsAsync(hotelId);
        var room = rooms.FirstOrDefault(r => r.Id == roomId);
        if (room is null) return NotFound();

        ViewData["Title"] = "Edit Room";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";

        return View(new RoomFormViewModel
        {
            Id = room.Id,
            HotelId = hotelId,
            Name = room.Name,
            RoomType = room.RoomType,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            IsAvailable = room.IsAvailable
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoom(RoomFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await _api.UpdateRoomAsync(model.HotelId, model.Id, new UpdateRoomRequest
        {
            Name = model.Name,
            RoomType = model.RoomType,
            PricePerNight = model.PricePerNight,
            Capacity = model.Capacity
        });

        await _api.ToggleRoomAvailabilityAsync(model.HotelId, model.Id, model.IsAvailable);

        return RedirectToAction(nameof(Rooms), new { id = model.HotelId });
    }

    public async Task<IActionResult> Bookings()
    {
        ViewData["Title"] = "Hotel Bookings";
        ViewData["Role"] = "Hotel Owner";
        ViewData["Portal"] = "Owner";
        return View(await _api.GetBookingsAsync());
    }
}
