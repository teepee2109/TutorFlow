using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev D — Lịch học và điểm danh từ góc nhìn học viên (F5, F8).
/// </summary>
[Authorize(Roles = "Student")]
public class StudentScheduleController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Lịch học của tôi";
        return View();
    }
}
