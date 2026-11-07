using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

[Authorize(Roles = "GrandManager,Lecturer")]
public sealed class TuitionsController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public TuitionsController(TutorFlowApiClient apiClient) => _apiClient = apiClient;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var periodFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var periodTo = to ?? periodFrom.AddMonths(1).AddDays(-1);
        var previewTask = _apiClient.GetTuitionPreviewAsync(periodFrom, periodTo);
        var recordsTask = _apiClient.GetTuitionsAsync(periodFrom, periodTo);
        await Task.WhenAll(previewTask, recordsTask);
        var preview = await previewTask;
        var records = await recordsTask;
        if (!preview.Success) ViewBag.PreviewErrorMessage = preview.Message;
        if (!records.Success) ViewBag.ErrorMessage = records.Message;

        ViewData["Title"] = "Học phí & Thanh toán";
        return View(new TuitionsPageViewModel
        {
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            Preview = preview.Value ?? new List<TuitionPreviewItemViewModel>(),
            Records = records.Value ?? new List<TuitionRecordViewModel>()
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
}
