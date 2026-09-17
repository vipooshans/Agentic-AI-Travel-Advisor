using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminController : Controller
{
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Admin Dashboard";
        ViewData["Role"] = "Administrator";
        ViewData["Portal"] = "Admin";
        return View();
    }
}
