using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Lecturer;
using BackEnd.Dtos.Rooms;
using BackEnd.Services.Common;
using BackEnd.Services.Lecturer;
using BackEnd.Services.Rooms;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/room-change-requests")]
public sealed class RoomChangeRequestsController : ControllerBase
{
    private readonly IChangeRequestReviewService _reviewService;
    private readonly IChangeRequestService _changeRequestService;
    private readonly ICurrentUser _currentUser;

    public RoomChangeRequestsController(
        IChangeRequestReviewService reviewService,
        IChangeRequestService changeRequestService,
        ICurrentUser currentUser)
    {
        _reviewService = reviewService;
        _changeRequestService = changeRequestService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> ListPending()
    {
        var result = await _reviewService.ListPendingAsync();
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<RoomChangeReviewDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<RoomChangeReviewDto>>.Fail(result.Message));
    }

    [HttpPost("{requestId:int}/approve")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> Approve(int requestId, ReviewNoteRequest request)
    {
        var result = await _reviewService.ApproveAsync(requestId, request.ReviewNote);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("{requestId:int}/reject")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> Reject(int requestId, RejectRoomChangeRequest request)
    {
        var result = await _reviewService.RejectAsync(requestId, request.ReviewNote);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpGet("options")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetCreateOptions()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<LecturerChangeRequestOptionsDto>.Fail("Không xác định được giảng viên đang đăng nhập."));
        }
        var result = await _changeRequestService.GetCreateOptionsAsync(_currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<LecturerChangeRequestOptionsDto>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<LecturerChangeRequestOptionsDto>.Fail(result.Message));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> ListMine()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<List<LecturerRoomChangeRequestDto>>.Fail("Không xác định được giảng viên đang đăng nhập."));
        }
        var result = await _changeRequestService.ListMineAsync(_currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<LecturerRoomChangeRequestDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<LecturerRoomChangeRequestDto>>.Fail(result.Message));
    }

    [HttpPost]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create(LecturerChangeRequestCreateDto request)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<LecturerRoomChangeRequestDto>.Fail("Không xác định được giảng viên đang đăng nhập."));
        }
        var result = await _changeRequestService.CreateAsync(request, _currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<LecturerRoomChangeRequestDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<LecturerRoomChangeRequestDto>.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        ScheduleError.RoomConflict or ScheduleError.LecturerConflict or ScheduleError.SessionDuplicate
            or ScheduleError.RoomClosed or ScheduleError.RoomHasSchedule or ScheduleError.StudentSessionConflict
            or ScheduleError.StudentEnrollConflict or ScheduleError.AttendanceNotEnrolled
            or ScheduleError.AttendanceSessionInvalid => StatusCodes.Status409Conflict,
        ScheduleError.AttendanceForbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
