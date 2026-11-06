using FrontEnd.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public AdminController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _apiClient.GetAdminDashboardAsync(targetDate);
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
        }

        return View(result.Value ?? new FrontEnd.Models.AdminDashboardViewModel { TargetDate = targetDate });
    }
}
