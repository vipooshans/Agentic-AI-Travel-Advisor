using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Payments;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

/// <summary>Simulated payments. No real gateway is called and card numbers are never stored.</summary>
[ApiController]
[Route("api/bookings/{bookingId:int}/payments")]
[Authorize]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetAll(int bookingId, CancellationToken cancellationToken) =>
        Ok(await paymentService.ListAsync(User.ToUserContext(), bookingId, cancellationToken));

    /// <summary>Pays the outstanding amount. Card payments settle immediately; cash and bank transfer stay pending until the provider confirms.</summary>
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(int bookingId, [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await paymentService.CreateAsync(User.ToUserContext(), bookingId, request, cancellationToken);
        return Created($"/api/bookings/{bookingId}/payments/{payment.Id}", payment);
    }

    /// <summary>Provider or admin marks a pending cash/bank-transfer payment as completed or failed.</summary>
    [Authorize(Policy = AuthPolicies.ProviderOrAdmin)]
    [HttpPatch("{paymentId:int}/status")]
    public async Task<ActionResult<PaymentDto>> UpdateStatus(int bookingId, int paymentId, [FromBody] UpdatePaymentStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await paymentService.UpdateStatusAsync(User.ToUserContext(), bookingId, paymentId, request, cancellationToken));
}
