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
    public async Task<IActionResult> BulkPreview([Bind(Prefix = "BulkForm")] BulkEnrollmentRequestViewModel request)
    {
        var model = new EnrollmentsPageViewModel { BulkForm = request };
        ValidateBulkRequest(request);
        if (!ModelState.IsValid)
        {
            await LoadOptionsAndListAsync(model);
            ViewData["Title"] = "Ghi danh học viên";
            return View("Index", model);
        }

        var preview = await _apiClient.PreviewBulkEnrollmentAsync(request);
        if (!preview.Success || preview.Value == null)
            ModelState.AddModelError(string.Empty, preview.Message);
        else
            model.BulkCheck = preview.Value;

        await LoadOptionsAndListAsync(model);
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
    public async Task<IActionResult> BulkEnroll([Bind(Prefix = "BulkForm")] BulkEnrollmentRequestViewModel request)
    {
        var model = new EnrollmentsPageViewModel { BulkForm = request };
        ValidateBulkRequest(request);
        if (!ModelState.IsValid)
        {
            await LoadOptionsAndListAsync(model);
            ViewData["Title"] = "Ghi danh học viên";
            return View("Index", model);
        }

        var result = await _apiClient.EnrollStudentsAsync(request);
        if (!result.Success || result.Value == null)
            ModelState.AddModelError(string.Empty, result.Message);
        else
            model.BulkResult = result.Value;

        await LoadOptionsAndListAsync(model);
        ViewData["Title"] = "Ghi danh học viên";
        return View("Index", model);
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

    private void ValidateBulkRequest(BulkEnrollmentRequestViewModel request)
    {
        var studentIds = request.StudentIds ?? new List<int>();
        if (studentIds.Count == 0)
            ModelState.AddModelError("BulkForm.StudentIds", "Chọn ít nhất một học viên.");
        else if (studentIds.Distinct().Count() > 100)
            ModelState.AddModelError("BulkForm.StudentIds", "Mỗi lần chỉ có thể xử lý tối đa 100 học viên.");
        else if (studentIds.Any(studentId => studentId <= 0))
            ModelState.AddModelError("BulkForm.StudentIds", "Danh sách học viên không hợp lệ.");

        if (request.ClassId <= 0)
            ModelState.AddModelError("BulkForm.ClassId", "Vui lòng chọn lớp.");
        if (request.EnrollDate == default)
            ModelState.AddModelError("BulkForm.EnrollDate", "Vui lòng chọn ngày ghi danh.");
    }
}
