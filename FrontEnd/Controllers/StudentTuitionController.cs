using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

[Authorize(Roles = "Student")]
public sealed class StudentTuitionController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public StudentTuitionController(TutorFlowApiClient apiClient) => _apiClient = apiClient;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _apiClient.GetMyTuitionsAsync();
        if (!result.Success) ViewBag.ErrorMessage = result.Message;
        ViewData["Title"] = "Học phí của tôi";
        return View(new StudentTuitionPageViewModel
        {
            Records = result.Value ?? new List<TuitionRecordViewModel>()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPaymentRequest(CreatePaymentRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin xác nhận thanh toán chưa hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _apiClient.CreateMyPaymentRequestAsync(model);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
