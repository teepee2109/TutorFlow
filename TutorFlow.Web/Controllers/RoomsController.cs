using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dev C — Quản lý phòng, xếp phòng cho giảng viên, xét duyệt đổi phòng/ca (F3, F6, F7).
/// </summary>
[Authorize]
public class RoomsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Quản lý phòng học";
        return View();
    }
}
