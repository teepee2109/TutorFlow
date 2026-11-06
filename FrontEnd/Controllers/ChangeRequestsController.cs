using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>
/// Dev C — Yêu cầu đổi phòng/ca từ giảng viên, duyệt bởi Facility Manager (F7).
/// </summary>
[Authorize]
public class ChangeRequestsController : Controller
{
    /// <summary>
    /// Danh sách yêu cầu (Facility Manager xem) hoặc yêu cầu của mình (Lecturer xem).
    /// </summary>
    public IActionResult Index()
    {
        ViewData["Title"] = "Yêu cầu đổi phòng / ca";
        return View();
    }

    /// <summary>
    /// Lecturer gửi yêu cầu đổi phòng/ca cho một buổi học cụ thể.
    /// </summary>
    [Authorize(Roles = "Lecturer")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Gửi yêu cầu đổi phòng / ca";
        return View();
    }
}
