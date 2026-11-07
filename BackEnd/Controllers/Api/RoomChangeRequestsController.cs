using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Rooms;
using BackEnd.Services.Common;
using BackEnd.Services.Rooms;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "FacilityManager")]
[Route("api/room-change-requests")]
public sealed class RoomChangeRequestsController : ControllerBase
{
    private readonly IChangeRequestReviewService _reviewService;

    public RoomChangeRequestsController(IChangeRequestReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> ListPending()
    {
        var result = await _reviewService.ListPendingAsync();
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<RoomChangeReviewDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<RoomChangeReviewDto>>.Fail(result.Message));
    }

    [HttpPost("{requestId:int}/approve")]
    public async Task<IActionResult> Approve(int requestId, ReviewNoteRequest request)
    {
        var result = await _reviewService.ApproveAsync(requestId, request.ReviewNote);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("{requestId:int}/reject")]
    public async Task<IActionResult> Reject(int requestId, RejectRoomChangeRequest request)
    {
        var result = await _reviewService.RejectAsync(requestId, request.ReviewNote);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        ScheduleError.RoomConflict or ScheduleError.LecturerConflict or ScheduleError.SessionDuplicate
            or ScheduleError.RoomClosed or ScheduleError.RoomHasSchedule or ScheduleError.StudentSessionConflict
            or ScheduleError.StudentEnrollConflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
