using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController(IPackageService packageService) : ControllerBase
{
    /// <summary>Search approved packages. Admins see every approval state and may filter by it.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TravelPackageDto>>> GetAll([FromQuery] PackageSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await packageService.SearchAsync(User.ToOptionalUserContext(), query, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireTravelAgent)]
    [HttpGet("mine")]
    public async Task<ActionResult<List<TravelPackageDto>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await packageService.ListMineAsync(User.ToUserContext(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TravelPackageDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await packageService.GetAsync(User.ToOptionalUserContext(), id, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireTravelAgent)]
    [HttpPost]
    public async Task<ActionResult<TravelPackageDto>> Create([FromBody] CreatePackageRequest request, CancellationToken cancellationToken)
    {
        var package = await packageService.CreateAsync(User.ToUserContext(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = package.Id }, package);
    }

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TravelPackageDto>> Update(int id, [FromBody] UpdatePackageRequest request, CancellationToken cancellationToken) =>
        Ok(await packageService.UpdateAsync(User.ToUserContext(), id, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await packageService.DeleteAsync(User.ToUserContext(), id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpPost("{id:int}/activities")]
    public async Task<ActionResult<PackageActivityDto>> AddActivity(int id, [FromBody] CreateActivityRequest request, CancellationToken cancellationToken) =>
        Ok(await packageService.AddActivityAsync(User.ToUserContext(), id, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpDelete("{packageId:int}/activities/{activityId:int}")]
    public async Task<IActionResult> DeleteActivity(int packageId, int activityId, CancellationToken cancellationToken)
    {
        await packageService.DeleteActivityAsync(User.ToUserContext(), packageId, activityId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/approval")]
    public async Task<ActionResult<TravelPackageDto>> SetApproval(int id, [FromBody] UpdateApprovalRequest request, CancellationToken cancellationToken) =>
        Ok(await packageService.SetApprovalAsync(id, request, cancellationToken));
}
