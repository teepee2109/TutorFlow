using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>
/// Dev E — Học phí và thanh toán (F9).
/// </summary>
[Authorize]
public class TuitionsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Học phí & Thanh toán";
        return View();
    }
}
