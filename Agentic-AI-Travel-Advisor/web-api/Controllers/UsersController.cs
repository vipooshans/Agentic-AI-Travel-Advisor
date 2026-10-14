using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Contact and personal details of the signed-in user (any role).</summary>
    [HttpGet("me/profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken) =>
        Ok(await userService.GetProfileAsync(User.ToUserContext().UserId, cancellationToken));

    [HttpPut("me/profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UserProfileDto request, CancellationToken cancellationToken) =>
        Ok(await userService.UpdateProfileAsync(User.ToUserContext().UserId, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpGet("me/preferences")]
    public async Task<ActionResult<TravelPreferencesDto>> GetPreferences(CancellationToken cancellationToken) =>
        Ok(await userService.GetPreferencesAsync(User.ToUserContext().UserId, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPut("me/preferences")]
    public async Task<ActionResult<TravelPreferencesDto>> UpdatePreferences([FromBody] TravelPreferencesDto request, CancellationToken cancellationToken) =>
        Ok(await userService.UpdatePreferencesAsync(User.ToUserContext().UserId, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] string? role, CancellationToken cancellationToken) =>
        Ok(await userService.ListAsync(role, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateStaffUserRequest request, CancellationToken cancellationToken) =>
        Ok(await userService.CreateStaffAsync(request, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPatch("{id}/active")]
    public async Task<ActionResult<UserDto>> SetActive(string id, [FromBody] UpdateUserActiveRequest request, CancellationToken cancellationToken) =>
        Ok(await userService.SetActiveAsync(User.ToUserContext(), id, request.IsActive, cancellationToken));
}
