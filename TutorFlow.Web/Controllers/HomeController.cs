using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TutorFlow.Services.Common;
using TutorFlow.Web.Models;

namespace TutorFlow.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICurrentUser _currentUser;

    public HomeController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public IActionResult Index()
    {
        // Redirect authenticated users to their role-specific dashboard
        if (_currentUser.IsAuthenticated)
        {
            return _currentUser.Role switch
            {
                "Admin"           => RedirectToAction("Index", "Admin"),
                "GrandManager"    => RedirectToAction("Index", "GrandManager"),
                "FacilityManager" => RedirectToAction("Index", "FacilityManager"),
                "Lecturer"        => RedirectToAction("Index", "Schedule"),
                "Student"         => RedirectToAction("Index", "StudentSchedule"),
                _                 => View()
            };
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
