using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev E — Xem học phí của học viên (F9).
/// </summary>
[Authorize(Roles = "Student")]
public class StudentTuitionController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Học phí của tôi";
        return View();
    }
}
