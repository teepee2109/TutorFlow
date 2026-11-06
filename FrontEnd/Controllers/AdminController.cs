using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorFlow.Web.Services.Api;

namespace TutorFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public AdminController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _apiClient.GetAdminDashboardAsync();
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
        }

        return View(result.Value ?? new TutorFlow.Web.Models.AdminDashboardViewModel());
    }
}
