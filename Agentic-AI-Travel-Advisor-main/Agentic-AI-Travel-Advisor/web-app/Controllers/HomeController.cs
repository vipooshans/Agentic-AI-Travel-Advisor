using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Web.Models;

namespace TravelAdvisor.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return role switch
            {
                RoleNames.Admin => RedirectToAction("Dashboard", "Admin"),
                RoleNames.HotelOwner => RedirectToAction("Dashboard", "Owner"),
                RoleNames.TravelAgent => RedirectToAction("Dashboard", "Agent"),
                _ => RedirectToAction("AccessDenied", "Account")
            };
        }

        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
