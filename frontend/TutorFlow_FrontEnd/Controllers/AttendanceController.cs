using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev D — Điểm danh từng buổi học (F8).
/// </summary>
[Authorize]
public class AttendanceController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Điểm danh";
        return View();
    }
}
