using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Rooms;
using BackEnd.Services.Common;
using BackEnd.Services.Rooms;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,GrandManager,FacilityManager,Lecturer")]
[Route("api/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] DateOnly? date, [FromQuery] int? facilityId)
    {
        var result = await _roomService.GetStatusGridAsync(facilityId, date ?? DateOnly.FromDateTime(DateTime.Today));
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<BackEnd.Dtos.RoomShiftStatusDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<BackEnd.Dtos.RoomShiftStatusDto>>.Fail(result.Message));
    }

    [HttpGet("empty")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> FindEmpty([FromQuery] DateOnly date, [FromQuery] int shiftId)
    {
        var result = await _roomService.FindEmptyRoomsAsync(date, shiftId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<RoomOptionDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<RoomOptionDto>>.Fail(result.Message));
    }

    [HttpGet("{roomId:int}/affected-sessions")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> GetAffectedSessions(int roomId)
    {
        var result = await _roomService.GetAffectedSessionsAsync(roomId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<AffectedSessionDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<AffectedSessionDto>>.Fail(result.Message));
    }

    [HttpPut("{roomId:int}/condition")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> SetCondition(int roomId, RoomConditionRequest request)
    {
        var result = await _roomService.SetConditionAsync(roomId, request.Condition);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPut("sessions/{sessionId:int}/room")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> AssignSessionRoom(int sessionId, RoomAssignmentRequest request)
    {
        var result = await _roomService.AssignSessionRoomAsync(sessionId, request.RoomId, request.ApplyToWholeClass);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("sessions/{sessionId:int}/cancel")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> CancelAffectedSession(int sessionId)
    {
        var result = await _roomService.CancelAffectedSessionAsync(sessionId);
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
