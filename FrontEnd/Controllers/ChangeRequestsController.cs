using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

/// <summary>
/// Facility Manager review queue for lecturer room and shift requests (F8).
/// </summary>
[Authorize]
public sealed class ChangeRequestsController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public ChangeRequestsController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Pending requests scoped to the Facility Manager's assigned facility.
    /// </summary>
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> Index()
    {
        var result = await _apiClient.GetPendingRoomChangeRequestsAsync();
        if (!result.Success) ViewBag.ErrorMessage = result.Message;
        ViewData["Title"] = "Yêu cầu đổi phòng / ca";
        return View(new RoomChangeReviewListViewModel
        {
            Requests = result.Value ?? new List<RoomChangeReviewViewModel>()
        });
    }

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int requestId, string? reviewNote)
    {
        var result = await _apiClient.ApproveRoomChangeRequestAsync(requestId, reviewNote);
        SetMessage(result);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectRoomChangeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Lý do từ chối bắt buộc và tối đa 255 ký tự.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _apiClient.RejectRoomChangeRequestAsync(model.RequestId, model.ReviewNote);
        SetMessage(result);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Mine()
    {
        var result = await _apiClient.GetMyRoomChangeRequestsAsync();
        if (!result.Success) ViewBag.ErrorMessage = result.Message;
        ViewData["Title"] = "Yêu cầu đổi phòng/ca của tôi";
        return View(new LecturerRoomChangeRequestListViewModel
        {
            Requests = result.Value ?? new List<LecturerRoomChangeRequestViewModel>()
        });
    }

    /// <summary>Lecturer submits a room or shift change request.</summary>
    [Authorize(Roles = "Lecturer")]
    [HttpGet]
    public async Task<IActionResult> Create(int? sessionId = null, int? classId = null)
    {
        var model = new CreateRoomChangeRequestViewModel();
        await LoadCreateOptionsAsync(model);
        var selectedScope = sessionId.HasValue
            ? model.Options.Scopes.FirstOrDefault(item => item.SessionId == sessionId.Value)
            : classId.HasValue
                ? model.Options.Scopes.FirstOrDefault(item => item.ScopeType == "Class" && item.ClassId == classId.Value)
                : model.Options.Scopes.FirstOrDefault();
        model.ScopeKey = selectedScope?.Key ?? string.Empty;
        ViewData["Title"] = "Gửi yêu cầu đổi phòng / ca";
        return View(model);
    }

    [Authorize(Roles = "Lecturer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoomChangeRequestViewModel model)
    {
        await LoadCreateOptionsAsync(model);
        var scope = model.Options.Scopes.FirstOrDefault(item => item.Key == model.ScopeKey);
        if (scope == null)
        {
            ModelState.AddModelError(nameof(model.ScopeKey), "Phạm vi đã chọn không hợp lệ.");
        }
        if (!model.RequestedRoomId.HasValue && !model.RequestedShiftId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Chọn ít nhất một phòng hoặc ca mới.");
        }
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Gửi yêu cầu đổi phòng / ca";
            return View(model);
        }

        var result = await _apiClient.CreateLecturerRoomChangeRequestAsync(
            scope!.ClassId, scope.SessionId, model.RequestedRoomId, model.RequestedShiftId, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewData["Title"] = "Gửi yêu cầu đổi phòng / ca";
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Mine));
    }

    private void SetMessage(ApiResponse result)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
    }

    private async Task LoadCreateOptionsAsync(CreateRoomChangeRequestViewModel model)
    {
        var result = await _apiClient.GetLecturerChangeRequestOptionsAsync();
        if (result.Success && result.Value != null)
        {
            model.Options = result.Value;
        }
        else
        {
            model.Options = new LecturerChangeRequestOptionsViewModel();
            ViewBag.OptionsErrorMessage = result.Message;
        }
    }
}
