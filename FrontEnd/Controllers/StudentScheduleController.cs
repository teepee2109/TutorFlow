using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

[Authorize(Roles = "Student")]
public sealed class StudentScheduleController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public StudentScheduleController(TutorFlowApiClient apiClient) => _apiClient = apiClient;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = from ?? today.AddDays(-7);
        var toDate = to ?? fromDate.AddDays(30);
        var result = await _apiClient.GetMyStudentScheduleAsync(fromDate, toDate);
        if (!result.Success) ViewBag.ErrorMessage = result.Message;
        ViewData["Title"] = "Lịch học của tôi";
        return View(new StudentSchedulePageViewModel
        {
            From = fromDate,
            To = toDate,
            Sessions = result.Value ?? new List<StudentPortalScheduleItemViewModel>()
        });
    }
}
