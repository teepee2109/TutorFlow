using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dashboard trang chủ cho Grand Manager — thống kê toàn hệ thống, quản lý người dùng (F1, F10).
/// </summary>
[Authorize(Roles = "GrandManager")]
public class GrandManagerController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Grand Manager Dashboard";
        return View();
    }
}
