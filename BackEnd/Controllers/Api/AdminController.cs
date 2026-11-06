using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Services.Dashboard;
using BackEnd.Services.Common;

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
    public async Task<IActionResult> GetDashboard([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _dashboardService.GetAdminStatsAsync(targetDate);
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
