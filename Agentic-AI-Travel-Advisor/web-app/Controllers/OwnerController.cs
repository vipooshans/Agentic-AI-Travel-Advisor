using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "HOTEL_OWNER")]
public class OwnerController : Controller
{
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Hotel Owner Dashboard";
        ViewData["Role"] = "Hotel Owner";
        return View();
    }
}
