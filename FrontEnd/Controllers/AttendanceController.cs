using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

/// <summary>Lecturers edit attendance; managers browse attendance records within their scope.</summary>
[Authorize(Roles = "GrandManager,FacilityManager,Lecturer")]
public sealed class AttendanceController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public AttendanceController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? sessionId, DateOnly? from, DateOnly? to)
    {
        if (!sessionId.HasValue)
        {
            if (!User.IsInRole("FacilityManager") && !User.IsInRole("GrandManager"))
            {
                return RedirectToAction("Index", "Schedule");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var end = to ?? today;
            var start = from ?? end.AddDays(-30);
            var sessions = await _apiClient.GetAttendanceSessionsAsync(start, end);
            if (!sessions.Success) ViewBag.ErrorMessage = sessions.Message;

            ViewData["Title"] = "Theo dõi điểm danh";
            return View("Sessions", new AttendanceSessionsPageViewModel
            {
                From = start,
                To = end,
                Sessions = sessions.Value ?? new List<AttendanceSessionListViewModel>()
            });
        }

        var result = await _apiClient.GetAttendanceRosterAsync(sessionId.Value);
        if (!result.Success || result.Value == null)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(new AttendanceRosterViewModel { SessionId = sessionId.Value });
        }

        ViewData["Title"] = User.IsInRole("Lecturer") ? "Điểm danh buổi học" : "Xem điểm danh";
        return View(result.Value);
    }

    [HttpPost]
    [Authorize(Roles = "Lecturer")]
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
    [Authorize(Roles = "Lecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int sessionId)
    {
        var result = await _apiClient.CompleteAttendanceSessionAsync(sessionId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { sessionId });
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
