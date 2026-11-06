using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Services.Dashboard;
using BackEnd.Services.Common;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "FacilityManager")]
[Route("api/facility-manager/dashboard")]
public sealed class FacilityManagerDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly ICurrentUser _currentUser;

    public FacilityManagerDashboardController(IAdminDashboardService dashboardService, ICurrentUser currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] DateOnly? date)
    {
        if (!_currentUser.FacilityId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<AdminDashboardDto>.Fail("Tài khoản Facility Manager chưa được gán cơ sở."));
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _dashboardService.GetFacilityStatsAsync(_currentUser.FacilityId.Value, targetDate);
        if (!result.Success || result.Value == null)
        {
            var response = ApiResponse<AdminDashboardDto>.Fail(result.Message);
            return result.Error == ScheduleError.NotFound
                ? NotFound(response)
                : StatusCode(StatusCodes.Status500InternalServerError, response);
        }

        return Ok(ApiResponse<AdminDashboardDto>.Ok(result.Value));
    }
}
