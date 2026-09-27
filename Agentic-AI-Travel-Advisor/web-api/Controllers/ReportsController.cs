using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> Summary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role);
        var dto = new ReportSummaryDto();

        var bookings = _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.Hotel)
            .Include(b => b.TravelPackage)
            .AsQueryable();

        bookings = role switch
        {
            RoleNames.Admin => bookings,
            RoleNames.HotelOwner => bookings.Where(b => b.Room != null && b.Room.Hotel.OwnerId == userId),
            RoleNames.TravelAgent => bookings.Where(b => b.TravelPackage != null && b.TravelPackage.AgentId == userId),
            _ => bookings.Where(b => b.UserId == userId)
        };

        var bookingList = await bookings.ToListAsync();
        dto.PendingBookings = bookingList.Count(b => b.Status == BookingStatus.Pending);
        dto.ConfirmedBookings = bookingList.Count(b => b.Status == BookingStatus.Confirmed);
        dto.CancelledBookings = bookingList.Count(b => b.Status == BookingStatus.Cancelled);
        dto.CompletedBookings = bookingList.Count(b => b.Status == BookingStatus.Completed);
        dto.Revenue = bookingList
            .Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Completed)
            .Sum(b => b.TotalPrice);

        if (role == RoleNames.Admin)
        {
            dto.UserCount = await _context.Users.CountAsync();
            dto.HotelOwnerCount = await _context.Users.CountAsync(u => u.Role.Name == RoleNames.HotelOwner);
            dto.TravelAgentCount = await _context.Users.CountAsync(u => u.Role.Name == RoleNames.TravelAgent);
            dto.HotelCount = await _context.Hotels.CountAsync();
            dto.PackageCount = await _context.TravelPackages.CountAsync();
            dto.PendingHotelApprovals = await _context.Hotels.CountAsync(h => h.ApprovalStatus == ApprovalStatus.Pending);
            dto.PendingPackageApprovals = await _context.TravelPackages.CountAsync(p => p.ApprovalStatus == ApprovalStatus.Pending);
        }
        else if (role == RoleNames.HotelOwner)
        {
            dto.HotelCount = await _context.Hotels.CountAsync(h => h.OwnerId == userId);
            dto.PendingHotelApprovals = await _context.Hotels.CountAsync(h => h.OwnerId == userId && h.ApprovalStatus == ApprovalStatus.Pending);
        }
        else if (role == RoleNames.TravelAgent)
        {
            dto.PackageCount = await _context.TravelPackages.CountAsync(p => p.AgentId == userId);
            dto.PendingPackageApprovals = await _context.TravelPackages.CountAsync(p => p.AgentId == userId && p.ApprovalStatus == ApprovalStatus.Pending);
        }

        return Ok(dto);
    }
}
