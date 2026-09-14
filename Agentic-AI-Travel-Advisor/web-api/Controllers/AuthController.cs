using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext context,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var userRole = await _context.AppRoles.FirstOrDefaultAsync(r => r.Name == RoleNames.User);
        if (userRole is null)
        {
            return StatusCode(500, new { message = "Default user role not found." });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return BadRequest(new { message = "Email is already registered." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = userRole.Id,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Registration failed.", errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(await BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(await BuildAuthResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _userManager.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(MapToUserDto(user));
    }

    private async Task<AuthResponse> BuildAuthResponse(ApplicationUser user)
    {
        if (user.Role is null)
        {
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
        }

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, user.Role!.Name);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role?.Name ?? string.Empty
    };
}
