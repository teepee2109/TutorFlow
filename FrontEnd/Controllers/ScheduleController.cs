using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

/// <summary>Lecturer's assigned sessions over a selected date range.</summary>
[Authorize(Roles = "Lecturer")]
public sealed class ScheduleController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public ScheduleController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var fromDate = from ?? monday;
        var toDate = to ?? fromDate.AddDays(6);
        var result = await _apiClient.GetLecturerScheduleAsync(fromDate, toDate);
        if (!result.Success) ViewBag.ErrorMessage = result.Message;

        ViewData["Title"] = "Lịch dạy của tôi";
        return View(new LecturerScheduleViewModel
        {
            From = fromDate,
            To = toDate,
            Sessions = result.Value ?? new List<LecturerScheduleSessionViewModel>()
        });
    }
}
