using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev D — Thời khóa biểu và điểm danh (F5, F8).
/// </summary>
[Authorize]
public class ScheduleController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Thời khóa biểu";
        return View();
    }
}
