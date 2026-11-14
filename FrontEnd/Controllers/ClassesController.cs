using FrontEnd.Models;
using FrontEnd.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>FrontEnd MVC controller for the Grand Manager's class management screen.</summary>
[Authorize(Roles = "GrandManager")]
public sealed class ClassesController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public ClassesController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(string? search, string? status, int? subjectId, int? lecturerId)
    {
        var classes = await _apiClient.GetClassesAsync(search, status, subjectId, lecturerId);
        var options = await _apiClient.GetClassOptionsAsync();
        if (!classes.Success) ViewBag.ErrorMessage = classes.Message;
        if (!options.Success) ViewBag.OptionsErrorMessage = options.Message;

        ViewData["Title"] = "Quản lý lớp học";
        return View(new ClassesIndexViewModel
        {
            Classes = classes.Value ?? new List<ClassListItemViewModel>(),
            Options = options.Value ?? new ClassOptionsViewModel(),
            Search = search ?? string.Empty,
            Status = status is "Active" or "Closed" ? status : "All",
            SubjectId = subjectId,
            LecturerId = lecturerId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ClassWriteViewModel();
        await LoadOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassWriteViewModel model)
    {
        await LoadOptionsAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        var preview = await _apiClient.PreviewClassConflictsAsync(model);
        if (!preview.Success)
        {
            ModelState.AddModelError(string.Empty, preview.Message);
            return View("Form", model);
        }
        if (preview.Value is { Count: > 0 })
        {
            model.Conflicts = preview.Value;
            ModelState.AddModelError(string.Empty, "Lịch có xung đột. Hãy đổi phòng, ca hoặc giảng viên trước khi lưu.");
            return View("Form", model);
        }

        var result = await _apiClient.CreateClassAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", model);
        }

        TempData["SuccessMessage"] = $"Đã tạo lớp và sinh {result.Value?.SessionCount ?? 0} buổi học.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _apiClient.GetClassAsync(id);
        if (!result.Success || result.Value == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var model = new ClassWriteViewModel
        {
            ClassId = result.Value.ClassId,
            ClassName = result.Value.ClassName,
            SubjectId = result.Value.SubjectId,
            LecturerId = result.Value.LecturerId,
            FacilityId = result.Value.FacilityId,
            RoomId = result.Value.RoomId,
            ShiftId = result.Value.ShiftId,
            DaysOfWeek = result.Value.DaysOfWeek,
            StartDate = result.Value.StartDate,
            EndDate = result.Value.EndDate
        };
        await LoadOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassWriteViewModel model)
    {
        model.ClassId = id;
        await LoadOptionsAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        var preview = await _apiClient.PreviewClassConflictsAsync(model, id);
        if (!preview.Success)
        {
            ModelState.AddModelError(string.Empty, preview.Message);
            return View("Form", model);
        }
        if (preview.Value is { Count: > 0 })
        {
            model.Conflicts = preview.Value;
            ModelState.AddModelError(string.Empty, "Lịch mới có xung đột. Hãy đổi phòng, ca hoặc giảng viên.");
            return View("Form", model);
        }

        var result = await _apiClient.UpdateClassAsync(id, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _apiClient.GetClassAsync(id);
        if (!result.Success || result.Value == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var options = await _apiClient.GetClassOptionsAsync();
        if (options.Success && options.Value != null)
        {
            result.Value.Options = options.Value;
        }
        else
        {
            ViewBag.OptionsErrorMessage = options.Message;
        }
        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateMissingSessions(int classId)
    {
        SetMessage(await _apiClient.GenerateClassSessionsAsync(classId));
        return RedirectToAction(nameof(Details), new { id = classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeLecturer(int classId, int lecturerId)
    {
        SetMessage(await _apiClient.ChangeClassLecturerAsync(classId, lecturerId));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int classId)
    {
        SetMessage(await _apiClient.CloseClassAsync(classId));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int classId, int sessionId, SessionRescheduleViewModel model)
    {
        SetMessage(await _apiClient.RescheduleSessionAsync(sessionId, model));
        return RedirectToAction(nameof(Details), new { id = classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSession(int classId, int sessionId)
    {
        SetMessage(await _apiClient.CancelSessionAsync(sessionId));
        return RedirectToAction(nameof(Details), new { id = classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubstitute(int classId, int sessionId, int lecturerId)
    {
        SetMessage(await _apiClient.AssignSessionSubstituteAsync(sessionId, lecturerId));
        return RedirectToAction(nameof(Details), new { id = classId });
    }

    private async Task LoadOptionsAsync(ClassWriteViewModel model)
    {
        var result = await _apiClient.GetClassOptionsAsync();
        if (result.Success && result.Value != null)
        {
            model.Options = result.Value;
        }
        else
        {
            ViewBag.OptionsErrorMessage = result.Message;
            model.Options = new ClassOptionsViewModel();
        }
    }

    private void SetMessage(ApiResponse result)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
    }
}
