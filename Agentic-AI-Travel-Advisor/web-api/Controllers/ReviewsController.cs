using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Reviews;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Authorize]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    /// <summary>Visible reviews of a hotel. Public.</summary>
    [AllowAnonymous]
    [HttpGet("api/hotels/{hotelId:int}/reviews")]
    public async Task<ActionResult<List<ReviewDto>>> ForHotel(int hotelId, CancellationToken cancellationToken) =>
        Ok(await reviewService.ListForHotelAsync(User.ToOptionalUserContext(), hotelId, cancellationToken));

    /// <summary>Visible reviews of a travel package. Public.</summary>
    [AllowAnonymous]
    [HttpGet("api/packages/{packageId:int}/reviews")]
    public async Task<ActionResult<List<ReviewDto>>> ForPackage(int packageId, CancellationToken cancellationToken) =>
        Ok(await reviewService.ListForPackageAsync(User.ToOptionalUserContext(), packageId, cancellationToken));

    /// <summary>Admins see all reviews, providers see visible reviews of their listings, guests see their own.</summary>
    [HttpGet("api/reviews")]
    public async Task<ActionResult<List<ReviewDto>>> GetAll([FromQuery] ReviewQuery query, CancellationToken cancellationToken) =>
        Ok(await reviewService.ListAsync(User.ToUserContext(), query, cancellationToken));

    /// <summary>Reviews a completed booking. One review per booking.</summary>
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPost("api/reviews")]
    public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await reviewService.CreateAsync(User.ToUserContext(), request, cancellationToken);
        return Created($"/api/reviews/{review.Id}", review);
    }

    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPut("api/reviews/{id:int}")]
    public async Task<ActionResult<ReviewDto>> Update(int id, [FromBody] UpdateReviewRequest request, CancellationToken cancellationToken) =>
        Ok(await reviewService.UpdateAsync(User.ToUserContext(), id, request, cancellationToken));

    /// <summary>The author or an admin can delete a review.</summary>
    [HttpDelete("api/reviews/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await reviewService.DeleteAsync(User.ToUserContext(), id, cancellationToken);
        return NoContent();
    }

    /// <summary>Moderation: hide or restore a review.</summary>
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPatch("api/reviews/{id:int}/status")]
    public async Task<ActionResult<ReviewDto>> SetStatus(int id, [FromBody] UpdateReviewStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await reviewService.SetStatusAsync(id, request, cancellationToken));
}
