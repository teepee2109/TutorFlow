using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos;
using BackEnd.Dtos.Api;
using BackEnd.Services.Common;
using BackEnd.Services.Tuition;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,GrandManager,Lecturer")]
[Route("api/tuitions")]
public sealed class TuitionsController : ControllerBase
{
    private readonly ITuitionService _service;
    private readonly ICurrentUser _currentUser;

    public TuitionsController(ITuitionService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int? classId)
    {
        var result = await _service.PreviewAsync(from, to, classId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<TuitionPreviewItemDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<TuitionPreviewItemDto>>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int? classId)
    {
        var result = await _service.ListAsync(from, to, classId);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<TuitionRecordDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<TuitionRecordDto>>.Fail(result.Message));
    }

    [HttpPost("issue")]
    [Authorize(Roles = "GrandManager")]
    public async Task<IActionResult> Issue(TuitionIssueRequestDto request)
    {
        var result = await _service.IssueAsync(request);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<TuitionIssueResultDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<TuitionIssueResultDto>.Fail(result.Message));
    }

    [HttpPost("{tuitionId:int}/payments")]
    [Authorize(Roles = "GrandManager")]
    public async Task<IActionResult> RecordPayment(int tuitionId, PaymentCreateDto request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized(ApiResponse<TuitionRecordDto>.Fail("Không xác định được Grand Manager đang đăng nhập."));
        var result = await _service.RecordPaymentAsync(tuitionId, request, _currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<TuitionRecordDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<TuitionRecordDto>.Fail(result.Message));
    }

    [HttpGet("payment-requests/pending")]
    [Authorize(Roles = "GrandManager")]
    public async Task<IActionResult> PendingPaymentRequests()
    {
        var result = await _service.GetPendingPaymentRequestsAsync();
        return result.Success && result.Value != null
            ? Ok(ApiResponse<List<PaymentRequestDto>>.Ok(result.Value))
            : StatusCode(StatusFor(result.Error), ApiResponse<List<PaymentRequestDto>>.Fail(result.Message));
    }

    [HttpPost("payment-requests/{requestId:int}/review")]
    [Authorize(Roles = "GrandManager")]
    public async Task<IActionResult> ReviewPaymentRequest(int requestId, PaymentRequestReviewDto request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized(ApiResponse<PaymentRequestDto>.Fail("Unable to identify the signed-in Grand Manager."));
        var result = await _service.ReviewPaymentRequestAsync(requestId, request, _currentUser.UserId.Value);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<PaymentRequestDto>.Ok(result.Value, result.Message))
            : StatusCode(StatusFor(result.Error), ApiResponse<PaymentRequestDto>.Fail(result.Message));
    }

    private static int StatusFor(ScheduleError? error) => error switch
    {
        ScheduleError.NotFound => StatusCodes.Status404NotFound,
        ScheduleError.InvalidRole => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
