using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TravelAdvisor.Web.Services;

namespace TravelAdvisor.Web.Middleware;

/// <summary>
/// Turns API 401/403 answers into a sign-in or access-denied redirect instead of an error page.
/// </summary>
public class ApiAuthExceptionFilter(ApiAuthService authService) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiUnauthorizedException:
                // The portal cookie outlives the API token, so drop both; otherwise the login page
                // would see a signed-in user and send them straight back here.
                await authService.LogoutAsync();
                var request = context.HttpContext.Request;
                // After signing in the browser can only repeat a GET, so other methods return to the default page.
                object? routeValues = HttpMethods.IsGet(request.Method)
                    ? new { ReturnUrl = $"{request.PathBase}{request.Path}{request.QueryString}" }
                    : null;
                context.Result = new RedirectToActionResult("Login", "Account", routeValues);
                context.ExceptionHandled = true;
                break;

            case ApiForbiddenException:
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                context.ExceptionHandled = true;
                break;
        }
    }
}
