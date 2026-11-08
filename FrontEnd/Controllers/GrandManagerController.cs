using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

/// <summary>
/// Dashboard trang chủ cho Grand Manager — thống kê toàn hệ thống, quản lý người dùng (F1, F10).
/// </summary>
[Authorize(Roles = "GrandManager")]
public class GrandManagerController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public GrandManagerController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _apiClient.GetGrandManagerDashboardAsync(targetDate);
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
        }
        else
        {
            ViewBag.DashboardLoaded = true;
        }

        ViewData["Title"] = "Dashboard quản lý tổng";
        return View(result.Value ?? new AdminDashboardViewModel { TargetDate = targetDate });
    }
}
