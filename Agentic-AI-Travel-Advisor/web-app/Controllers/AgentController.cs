using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TravelAdvisor.Web.Controllers;

[Authorize(Roles = "TRAVEL_AGENT")]
public class AgentController : Controller
{
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Travel Agent Dashboard";
        ViewData["Role"] = "Travel Agent";
        return View();
    }
}
