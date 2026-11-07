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

    /// <summary>
    /// Lecturer gửi yêu cầu đổi phòng/ca cho một buổi học cụ thể.
    /// </summary>
    [Authorize(Roles = "Lecturer")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Gửi yêu cầu đổi phòng / ca";
        return View();
    }

    private void SetMessage(ApiResponse result)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
    }
}
