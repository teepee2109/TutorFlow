using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos;
using BackEnd.Dtos.Api;
using BackEnd.Services.Common;
using BackEnd.Services.Enrollment;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "GrandManager")]
[Route("api/enrollments")]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _service;

    public EnrollmentsController(IEnrollmentService service) => _service = service;

    [HttpGet("options")]
    public async Task<IActionResult> Options()
    {
        var result = await _service.GetOptionsAsync();
        return result.Success && result.Value != null
            ? Ok(ApiResponse<EnrollmentOptionsDto>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<EnrollmentOptionsDto>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int? classId, [FromQuery] string? status)
    {
        var result = await _service.ListAsync(search, classId, status);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<EnrollmentDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<EnrollmentDto>>.Fail(result.Message));
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(EnrollmentRequestDto request)
    {
        var result = await _service.PreviewConflictsAsync(request);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<EnrollmentPreviewDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<EnrollmentPreviewDto>.Fail(result.Message));
    }

    [HttpPost("bulk-preview")]
    public async Task<IActionResult> BulkPreview(BulkEnrollmentRequestDto request)
    {
        var result = await _service.PreviewManyAsync(request);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<BulkEnrollmentCheckDto>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<BulkEnrollmentCheckDto>.Fail(result.Message));
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(EnrollmentRequestDto request)
    {
        var result = await _service.EnrollAsync(request);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<EnrollmentDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<EnrollmentDto>.Fail(result.Message));
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkEnroll(BulkEnrollmentRequestDto request)
    {
        var result = await _service.EnrollManyAsync(request);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<BulkEnrollmentResultDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<BulkEnrollmentResultDto>.Fail(result.Message));
    }

    [HttpPut("{enrollmentId:int}/drop")]
    public async Task<IActionResult> Drop(int enrollmentId)
    {
        var result = await _service.DropAsync(enrollmentId);
        return result.Success ? Ok(ApiResponse.Ok(result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        ScheduleError.StudentEnrollConflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
