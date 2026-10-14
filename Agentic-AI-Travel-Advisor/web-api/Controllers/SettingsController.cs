using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Settings;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(ISystemSettingsService settingsService) : ControllerBase
{
    /// <summary>Client-facing settings (currency, AI on/off, maintenance banner, booking limits). Public.</summary>
    [HttpGet("public")]
    public async Task<ActionResult<PublicSettingsDto>> GetPublic(CancellationToken cancellationToken) =>
        Ok(await settingsService.GetPublicAsync(cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await settingsService.ListAsync(cancellationToken));

    /// <summary>Updates one known setting. Values are validated per key.</summary>
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPut("{key}")]
    public async Task<ActionResult<SystemSettingDto>> Update(string key, [FromBody] UpdateSystemSettingRequest request, CancellationToken cancellationToken) =>
        Ok(await settingsService.UpdateAsync(User.ToUserContext(), key, request, cancellationToken));
}
