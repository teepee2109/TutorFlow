using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Data;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Classes;
using Microsoft.EntityFrameworkCore;
using BackEnd.Services.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "GrandManager")]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPut("{sessionId:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int sessionId, SessionRescheduleDto request)
    {
        var result = await _sessionService.RescheduleSessionAsync(sessionId, request);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("{sessionId:int}/cancel")]
    public async Task<IActionResult> Cancel(int sessionId)
    {
        var result = await _sessionService.CancelSessionAsync(sessionId);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPut("{sessionId:int}/substitute")]
    public async Task<IActionResult> AssignSubstitute(int sessionId, AssignSubstituteDto request)
    {
        var result = await _sessionService.AssignSubstituteAsync(sessionId, request.LecturerId);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.RoomConflict or ScheduleError.LecturerConflict or ScheduleError.SessionDuplicate
            or ScheduleError.RoomClosed or ScheduleError.StudentSessionConflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
