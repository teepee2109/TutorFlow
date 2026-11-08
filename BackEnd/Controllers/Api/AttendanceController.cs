using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;
using BackEnd.Services.Lecturer;

namespace BackEnd.Controllers.Api;

/// <summary>Attendance reads for managers and lecturers, with writes restricted to lecturers.</summary>
[ApiController]
[Authorize(Roles = "GrandManager,FacilityManager,Lecturer")]
[Route("api/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ICurrentUser _currentUser;

    public AttendanceController(IAttendanceService attendanceService, ICurrentUser currentUser)
    {
        _attendanceService = attendanceService;
        _currentUser = currentUser;
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var end = to ?? today;
        var start = from ?? end.AddDays(-30);
        var result = await _attendanceService.GetSessionsAsync(start, end);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<AttendanceSessionListDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<AttendanceSessionListDto>>.Fail(result.Message));
    }

    [HttpGet("{sessionId:int}/roster")]
    public async Task<IActionResult> GetRoster(int sessionId)
    {
        var result = await _attendanceService.GetRosterAsync(sessionId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<AttendanceRosterDto>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<AttendanceRosterDto>.Fail(result.Message));
    }

    [HttpPut("{sessionId:int}")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Save(int sessionId, AttendanceSaveDto request)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("Không xác định được giảng viên đang đăng nhập."));
        }

        var result = await _attendanceService.SaveAsync(sessionId, request.Items, _currentUser.UserId.Value);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("{sessionId:int}/complete")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Complete(int sessionId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("Không xác định được giảng viên đang đăng nhập."));
        }

        var result = await _attendanceService.CompleteSessionAsync(sessionId, _currentUser.UserId.Value);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.AttendanceForbidden or ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        ScheduleError.AttendanceNotEnrolled or ScheduleError.AttendanceSessionInvalid
            or ScheduleError.SessionDuplicate => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
