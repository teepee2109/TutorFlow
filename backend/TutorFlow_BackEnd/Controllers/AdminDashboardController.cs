using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorFlow.BackEnd.Dtos;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Dashboard;

namespace TutorFlow.BackEnd.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminDashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<VwAdminStat>>> Get()
    {
        var result = await _dashboard.GetAdminStatsAsync();
        if (!result.Success || result.Value is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<VwAdminStat>.Fail(result.Message));
        }

        return Ok(ApiResponse<VwAdminStat>.Ok(result.Value));
    }
}
