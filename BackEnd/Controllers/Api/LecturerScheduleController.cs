using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;
using BackEnd.Services.Lecturer;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "Lecturer")]
[Route("api/lecturer/schedule")]
public sealed class LecturerScheduleController : ControllerBase
{
    private readonly ILecturerScheduleService _scheduleService;
    private readonly ICurrentUser _currentUser;

    public LecturerScheduleController(ILecturerScheduleService scheduleService, ICurrentUser currentUser)
    {
        _scheduleService = scheduleService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<List<LecturerScheduleSessionDto>>.Fail("Không xác định được giảng viên đang đăng nhập."));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var fromDate = from ?? monday;
        var toDate = to ?? fromDate.AddDays(6);
        var result = await _scheduleService.GetMyScheduleAsync(_currentUser.UserId.Value, fromDate, toDate);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<LecturerScheduleSessionDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<LecturerScheduleSessionDto>>.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.AttendanceForbidden or ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
