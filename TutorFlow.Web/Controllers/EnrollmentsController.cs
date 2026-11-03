using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev E — Ghi danh học viên vào lớp (F4).
/// </summary>
[Authorize]
public class EnrollmentsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Ghi danh học viên";
        return View();
    }
}
