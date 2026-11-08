using FrontEnd.Services;
using FrontEnd.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>
/// Dashboard trang chủ cho Facility Manager — thống kê cơ sở phụ trách (F3, F6, F7, F10).
/// </summary>
[Authorize(Roles = "FacilityManager")]
public class FacilityManagerController : Controller
{
    private readonly ICurrentUser _currentUser;
    private readonly TutorFlowApiClient _apiClient;

    public FacilityManagerController(ICurrentUser currentUser, TutorFlowApiClient apiClient)
    {
        _currentUser = currentUser;
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _apiClient.GetFacilityManagerDashboardAsync(targetDate);
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
        }
        else
        {
            ViewBag.DashboardLoaded = true;
        }

        ViewData["Title"] = "Facility Manager Dashboard";
        ViewData["FacilityId"] = _currentUser.FacilityId;
        return View(result.Value ?? new FrontEnd.Models.AdminDashboardViewModel
        {
            FacilityId = _currentUser.FacilityId,
            TargetDate = targetDate
        });
    }
}
