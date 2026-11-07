using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

[Authorize(Roles = "GrandManager")]
public sealed class EnrollmentsController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public EnrollmentsController(TutorFlowApiClient apiClient) => _apiClient = apiClient;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string status = "All", int? classId = null)
    {
        var model = new EnrollmentsPageViewModel { Search = search, Status = status, ClassFilterId = classId };
        await LoadOptionsAndListAsync(model);
        ViewData["Title"] = "Ghi danh học viên";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(EnrollmentsPageViewModel model)
    {
        await LoadOptionsAndListAsync(model);
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Ghi danh học viên";
            return View("Index", model);
        }

        var preview = await _apiClient.PreviewEnrollmentAsync(model.Form);
        if (!preview.Success || preview.Value == null)
        {
            ModelState.AddModelError(string.Empty, preview.Message);
        }
        else
        {
            model.Preview = preview.Value;
            if (preview.Value.AlreadyEnrolled)
                ModelState.AddModelError(string.Empty, "Học viên đã ghi danh vào lớp này.");
        }
        ViewData["Title"] = "Ghi danh học viên";
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(EnrollmentsPageViewModel model)
    {
        var result = await _apiClient.EnrollStudentAsync(model.Form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadOptionsAndListAsync(model);
            ViewData["Title"] = "Ghi danh học viên";
            return View("Index", model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Drop(int enrollmentId)
    {
        var result = await _apiClient.DropEnrollmentAsync(enrollmentId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int studentId, int classId)
    {
        var result = await _apiClient.EnrollStudentAsync(new EnrollmentRequestViewModel
        {
            StudentId = studentId,
            ClassId = classId,
            EnrollDate = DateOnly.FromDateTime(DateTime.Today)
        });
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOptionsAndListAsync(EnrollmentsPageViewModel model)
    {
        var options = await _apiClient.GetEnrollmentOptionsAsync();
        if (options.Success && options.Value != null) model.Options = options.Value;
        else ViewBag.OptionsErrorMessage = options.Message;

        var enrollments = await _apiClient.GetEnrollmentsAsync(model.Search, model.ClassFilterId, model.Status);
        if (enrollments.Success && enrollments.Value != null) model.Enrollments = enrollments.Value;
        else ViewBag.ErrorMessage = enrollments.Message;
    }
}
