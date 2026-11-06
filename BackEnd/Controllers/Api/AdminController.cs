using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using TutorFlow.Services.Dashboard;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _dashboardService.GetAdminStatsAsync();
        if (!result.Success || result.Value == null)
        {
            return NotFound(ApiResponse<AdminDashboardDto>.Fail(result.Message));
        }

        var stats = result.Value;
        var dto = new AdminDashboardDto
        {
            TotalRooms = stats.TotalRooms,
            OpenRooms = stats.OpenRooms,
            ClosedRooms = stats.ClosedRooms,
            ActiveClasses = stats.ActiveClasses,
            ActiveLecturers = stats.ActiveLecturers,
            ActiveStudents = stats.ActiveStudents,
            PendingChangeRequests = stats.PendingChangeRequests,
            TotalUnpaidTuition = stats.TotalUnpaidTuition
        };
        return Ok(ApiResponse<AdminDashboardDto>.Ok(dto));
    }
}
