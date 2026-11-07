using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

/// <summary>Attendance roster, save/update, and completion actions for lecturers.</summary>
[Authorize(Roles = "Lecturer")]
public sealed class AttendanceController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public AttendanceController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int sessionId)
    {
        var result = await _apiClient.GetAttendanceRosterAsync(sessionId);
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(new AttendanceRosterViewModel { SessionId = sessionId });
        }

        ViewData["Title"] = "Điểm danh buổi học";
        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceSaveViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ErrorMessage = "Dữ liệu điểm danh không hợp lệ.";
            return await ReloadWithSubmittedValuesAsync(model);
        }

        var result = await _apiClient.SaveAttendanceAsync(model.SessionId, model.Students);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { sessionId = model.SessionId });
        }

        ViewBag.ErrorMessage = result.Message;
        return await ReloadWithSubmittedValuesAsync(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int sessionId)
    {
        var result = await _apiClient.CompleteAttendanceSessionAsync(sessionId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction("Index", "Schedule");
    }

    private async Task<IActionResult> ReloadWithSubmittedValuesAsync(AttendanceSaveViewModel posted)
    {
        var roster = await _apiClient.GetAttendanceRosterAsync(posted.SessionId);
        if (!roster.Success || roster.Value == null)
        {
            return View("Index", new AttendanceRosterViewModel { SessionId = posted.SessionId });
        }

        var postedByStudent = posted.Students
            .GroupBy(item => item.StudentId)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var student in roster.Value.Students)
        {
            if (postedByStudent.TryGetValue(student.StudentId, out var postedStudent))
            {
                student.AttendanceStatus = postedStudent.AttendanceStatus;
                student.Note = postedStudent.Note;
            }
        }
        return View("Index", roster.Value);
    }
}
