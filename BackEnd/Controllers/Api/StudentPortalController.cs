using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos;
using BackEnd.Dtos.Api;
using BackEnd.Services.Common;
using BackEnd.Services.StudentPortal;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/student-portal")]
public sealed class StudentPortalController : ControllerBase
{
    private readonly IStudentPortalService _service;
    private readonly ICurrentUser _currentUser;

    public StudentPortalController(IStudentPortalService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!_currentUser.UserId.HasValue) return Unauthorized(ApiResponse<List<StudentPortalScheduleItemDto>>.Fail("Không xác định được học viên đang đăng nhập."));
        var range = DefaultRange(from, to);
        var result = await _service.GetMyScheduleAsync(_currentUser.UserId.Value, range.From, range.To);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<StudentPortalScheduleItemDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<StudentPortalScheduleItemDto>>.Fail(result.Message));
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!_currentUser.UserId.HasValue) return Unauthorized(ApiResponse<List<StudentPortalScheduleItemDto>>.Fail("Không xác định được học viên đang đăng nhập."));
        var range = DefaultRange(from, to);
        var result = await _service.GetMyAttendanceAsync(_currentUser.UserId.Value, range.From, range.To);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<StudentPortalScheduleItemDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<StudentPortalScheduleItemDto>>.Fail(result.Message));
    }

    [HttpGet("tuitions")]
    public async Task<IActionResult> Tuitions()
    {
        if (!_currentUser.UserId.HasValue) return Unauthorized(ApiResponse<List<TuitionRecordDto>>.Fail("Không xác định được học viên đang đăng nhập."));
        var result = await _service.GetMyTuitionAsync(_currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<TuitionRecordDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<TuitionRecordDto>>.Fail(result.Message));
    }

    [HttpPost("tuitions/{tuitionId:int}/payment-requests")]
    public async Task<IActionResult> CreatePaymentRequest(int tuitionId, PaymentRequestCreateDto request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized(ApiResponse<PaymentRequestDto>.Fail("Unable to identify the signed-in student."));
        var result = await _service.CreatePaymentRequestAsync(tuitionId, request, _currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<PaymentRequestDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<PaymentRequestDto>.Fail(result.Message));
    }

    private static (DateOnly From, DateOnly To) DefaultRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? today.AddDays(-30);
        return (start, to ?? start.AddDays(60));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
