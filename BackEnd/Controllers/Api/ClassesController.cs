using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Dtos.Classes;
using BackEnd.Services.Common;
using BackEnd.Services.Classes;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "GrandManager")]
[Route("api/classes")]
public sealed class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly ISessionService _sessionService;
    private readonly ICurrentUser _currentUser;

    public ClassesController(IClassService classService, ISessionService sessionService, ICurrentUser currentUser)
    {
        _classService = classService;
        _sessionService = sessionService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        var result = await _classService.GetClassesAsync();
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<ClassListItemDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<ClassListItemDto>>.Fail(result.Message));
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions()
    {
        var result = await _classService.GetOptionsAsync();
        return result.Success && result.Value != null
            ? Ok(ApiResponse<ClassOptionsDto>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<ClassOptionsDto>.Fail(result.Message));
    }

    [HttpGet("{classId:int}")]
    public async Task<IActionResult> GetClass(int classId)
    {
        var result = await _classService.GetClassAsync(classId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<ClassDetailDto>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<ClassDetailDto>.Fail(result.Message));
    }

    [HttpPost("preview-conflicts")]
    public async Task<IActionResult> PreviewConflicts(ClassWriteDto request, [FromQuery] int? excludeClassId)
    {
        var result = await _classService.PreviewConflictsAsync(request, excludeClassId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<ScheduleConflictDto>>.Ok(result.Value,
                result.Value.Count == 0 ? "Không phát hiện xung đột lịch." : "Có xung đột lịch cần xử lý."))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<ScheduleConflictDto>>.Fail(result.Message));
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass(ClassWriteDto request)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<ClassDetailDto>.Fail("Không xác định được tài khoản đang đăng nhập."));
        }

        var result = await _classService.CreateAsync(request, _currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<ClassDetailDto>.Ok(result.Value, "Đã tạo lớp và sinh các buổi học."))
            : StatusCode(StatusFor(result.Error), ApiResponse<ClassDetailDto>.Fail(result.Message));
    }

    [HttpPut("{classId:int}")]
    public async Task<IActionResult> UpdateClass(int classId, ClassWriteDto request)
    {
        var result = await _classService.UpdateAsync(classId, request);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<ClassDetailDto>.Ok(result.Value, "Đã cập nhật lớp và lịch các buổi sắp tới."))
            : StatusCode(StatusFor(result.Error), ApiResponse<ClassDetailDto>.Fail(result.Message));
    }

    [HttpPut("{classId:int}/lecturer")]
    public async Task<IActionResult> ChangeLecturer(int classId, ChangeLecturerDto request)
    {
        var result = await _classService.ChangeLecturerAsync(classId, request.LecturerId);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("{classId:int}/close")]
    public async Task<IActionResult> CloseClass(int classId)
    {
        var result = await _classService.CloseAsync(classId);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    [HttpPost("{classId:int}/generate-sessions")]
    public async Task<IActionResult> GenerateSessions(int classId)
    {
        var result = await _sessionService.GenerateForClassAsync(classId);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.RoomConflict or ScheduleError.LecturerConflict or ScheduleError.SessionDuplicate
            or ScheduleError.RoomClosed or ScheduleError.RoomHasSchedule or ScheduleError.StudentSessionConflict
            or ScheduleError.StudentEnrollConflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
