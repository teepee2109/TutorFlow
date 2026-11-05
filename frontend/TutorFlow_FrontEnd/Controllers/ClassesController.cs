using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev B — Quản lý lớp học và sinh buổi học (F2, F5).
/// </summary>
[Authorize]
public class ClassesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Quản lý lớp học";
        return View();
    }
}
