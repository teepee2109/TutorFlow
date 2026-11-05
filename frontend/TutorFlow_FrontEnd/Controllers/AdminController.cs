using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorFlow.Services.Dashboard;

namespace TutorFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _dashboardService.GetAdminStatsAsync();
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
        }

        return View(result.Value);
    }
}
