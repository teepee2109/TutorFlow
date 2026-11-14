using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

[Authorize(Roles = "Admin,GrandManager,Lecturer")]
public sealed class TuitionsController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public TuitionsController(TutorFlowApiClient apiClient) => _apiClient = apiClient;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var previousMonth = today.AddMonths(-1);
        var periodFrom = from ?? new DateOnly(previousMonth.Year, previousMonth.Month, 1);
        var periodTo = to ?? new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        var previewTask = _apiClient.GetTuitionPreviewAsync(periodFrom, periodTo);
        var recordsTask = _apiClient.GetTuitionsAsync(periodFrom, periodTo);
        var requestsTask = User.IsInRole("GrandManager")
            ? _apiClient.GetPendingPaymentRequestsAsync()
            : Task.FromResult(new ApiResponse<List<PaymentRequestViewModel>> { Success = true, Value = new List<PaymentRequestViewModel>() });
        await Task.WhenAll(previewTask, recordsTask, requestsTask);
        var preview = await previewTask;
        var records = await recordsTask;
        var requests = await requestsTask;
        if (!preview.Success) ViewBag.PreviewErrorMessage = preview.Message;
        if (!records.Success) ViewBag.ErrorMessage = records.Message;
        if (!requests.Success) ViewBag.PaymentRequestErrorMessage = requests.Message;

        ViewData["Title"] = "Học phí & Thanh toán";
        return View(new TuitionsPageViewModel
        {
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            Preview = preview.Value ?? new List<TuitionPreviewItemViewModel>(),
            Records = records.Value ?? new List<TuitionRecordViewModel>(),
            PendingPaymentRequests = requests.Value ?? new List<PaymentRequestViewModel>()
        });
    }

    [HttpPost]
    [Authorize(Roles = "GrandManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(TuitionIssueRequestViewModel model)
    {
        var result = await _apiClient.IssueTuitionsAsync(model);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { from = model.PeriodFrom, to = model.PeriodTo });
    }

    [HttpPost]
    [Authorize(Roles = "GrandManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(RecordPaymentViewModel model, DateOnly from, DateOnly to)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin thanh toán không hợp lệ.";
            return RedirectToAction(nameof(Index), new { from, to });
        }

        var result = await _apiClient.RecordTuitionPaymentAsync(model);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { from, to });
    }

    [HttpPost]
    [Authorize(Roles = "GrandManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewPaymentRequest(ReviewPaymentRequestViewModel model, DateOnly from, DateOnly to)
    {
        if ((model.Decision != "Approve" && model.Decision != "Reject") ||
            (model.Decision == "Reject" && string.IsNullOrWhiteSpace(model.ReviewNote)))
        {
            TempData["ErrorMessage"] = "Khi từ chối cần nhập lý do.";
            return RedirectToAction(nameof(Index), new { from, to });
        }

        var result = await _apiClient.ReviewPaymentRequestAsync(model.PaymentRequestId, model.Decision, model.ReviewNote);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { from, to });
    }
}
