using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public UsersController(UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [Authorize(Policy = "RequireUser")]
    [HttpGet("me/preferences")]
    public async Task<ActionResult<TravelPreferencesDto>> GetPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var prefs = await _context.TravelPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        return Ok(MapPrefs(prefs));
    }

    [Authorize(Policy = "RequireUser")]
    [HttpPut("me/preferences")]
    public async Task<ActionResult<TravelPreferencesDto>> UpdatePreferences([FromBody] TravelPreferencesDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var prefs = await _context.TravelPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs is null)
        {
            prefs = new TravelPreferences { UserId = userId };
            _context.TravelPreferences.Add(prefs);
        }

        prefs.BudgetMin = request.BudgetMin;
        prefs.BudgetMax = request.BudgetMax;
        prefs.PreferredClimate = request.PreferredClimate;
        prefs.Interests = request.Interests;
        await _context.SaveChangesAsync();
        return Ok(MapPrefs(prefs));
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] string? role)
    {
        var query = _userManager.Users.Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role.Name == role);

        var users = await query
            .OrderBy(u => u.Email)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.Name,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateStaffUserRequest request)
    {
        if (request.Role is not RoleNames.HotelOwner and not RoleNames.TravelAgent)
            return BadRequest(new { message = "Role must be HOTEL_OWNER or TRAVEL_AGENT." });

        var role = await _context.AppRoles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role is null)
            return StatusCode(500, new { message = "Role not found." });

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest(new { message = "Email is already registered." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = role.Id,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = "Could not create user.", errors = result.Errors.Select(e => e.Description) });

        user.Role = role;
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = role.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPatch("{id}/active")]
    public async Task<ActionResult<UserDto>> SetActive(string id, [FromBody] UpdateUserActiveRequest request)
    {
        var user = await _userManager.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return NotFound(new { message = "User not found." });

        user.IsActive = request.IsActive;
        await _userManager.UpdateAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    private static TravelPreferencesDto MapPrefs(TravelPreferences? prefs) => new()
    {
        BudgetMin = prefs?.BudgetMin,
        BudgetMax = prefs?.BudgetMax,
        PreferredClimate = prefs?.PreferredClimate,
        Interests = prefs?.Interests
    };
}
