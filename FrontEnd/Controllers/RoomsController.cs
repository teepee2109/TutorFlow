using FrontEnd.Models;
using FrontEnd.Services;
using FrontEnd.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>Room status grid and Facility Manager room operations (F6, F7 and F9).</summary>
[Authorize(Roles = "Admin,GrandManager,FacilityManager,Lecturer")]
public sealed class RoomsController : Controller
{
    private readonly TutorFlowApiClient _apiClient;
    private readonly ICurrentUser _currentUser;

    public RoomsController(TutorFlowApiClient apiClient, ICurrentUser currentUser)
    {
        _apiClient = apiClient;
        _currentUser = currentUser;
    }

    [HttpGet]
    public Task<IActionResult> Index(DateOnly? date, int? facilityId, int? affectedRoomId) =>
        BuildIndexAsync(date ?? DateOnly.FromDateTime(DateTime.Today), facilityId, affectedRoomId);

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCondition(int roomId, string condition, DateOnly date)
    {
        var result = await _apiClient.SetRoomConditionAsync(roomId, condition);
        SetMessage(result);
        return RedirectToAction(nameof(Index), new
        {
            date,
            affectedRoomId = !result.Success && condition == "Closed" ? roomId : (int?)null
        });
    }

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSessionRoom(
        int sessionId, int roomId, bool applyToWholeClass, DateOnly date, int? affectedRoomId)
    {
        SetMessage(await _apiClient.AssignRoomToSessionAsync(sessionId, roomId, applyToWholeClass));
        return RedirectToAction(nameof(Index), new { date, affectedRoomId });
    }

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAffectedSession(int sessionId, DateOnly date, int affectedRoomId)
    {
        SetMessage(await _apiClient.CancelAffectedRoomSessionAsync(sessionId));
        return RedirectToAction(nameof(Index), new { date, affectedRoomId });
    }

    private async Task<IActionResult> BuildIndexAsync(DateOnly date, int? facilityId, int? affectedRoomId)
    {
        var canManage = _currentUser.IsInRole("FacilityManager");
        if (canManage) facilityId = _currentUser.FacilityId;

        var result = await _apiClient.GetRoomStatusGridAsync(date, facilityId);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
        }

        var slots = result.Value ?? new List<RoomShiftStatusViewModel>();
        var roomActionShiftIds = slots.GroupBy(slot => slot.RoomId)
            .ToDictionary(group => group.Key, group => group.Min(slot => slot.ShiftId));
        var emptyRoomsByShift = new Dictionary<int, List<RoomOptionViewModel>>();
        if (canManage)
        {
            foreach (var shiftId in slots.Select(slot => slot.ShiftId).Distinct())
            {
                var emptyRooms = await _apiClient.FindEmptyRoomsAsync(date, shiftId);
                if (emptyRooms.Success && emptyRooms.Value != null)
                {
                    emptyRoomsByShift[shiftId] = emptyRooms.Value;
                }
                else
                {
                    ViewBag.EmptyRoomsErrorMessage = emptyRooms.Message;
                    emptyRoomsByShift[shiftId] = new List<RoomOptionViewModel>();
                }
            }
        }

        var model = new RoomGridViewModel
        {
            TargetDate = date,
            CanManageRooms = canManage,
            AffectedRoomId = affectedRoomId,
            Slots = slots.Select(slot => new RoomGridSlotViewModel
            {
                Slot = slot,
                ShowRoomConditionAction = roomActionShiftIds[slot.RoomId] == slot.ShiftId,
                EmptyRooms = emptyRoomsByShift.GetValueOrDefault(slot.ShiftId) ?? new List<RoomOptionViewModel>()
            }).ToList()
        };

        if (canManage && affectedRoomId.HasValue)
        {
            model.AffectedRoomNumber = slots.FirstOrDefault(slot => slot.RoomId == affectedRoomId.Value)?.RoomNumber;
            var affected = await _apiClient.GetAffectedRoomSessionsAsync(affectedRoomId.Value);
            if (affected.Success && affected.Value != null)
            {
                model.AffectedSessions = affected.Value;
            }
            else
            {
                ViewBag.AffectedSessionsErrorMessage = affected.Message;
            }
        }

        ViewData["Title"] = "Lưới trạng thái phòng";
        return View("Index", model);
    }

    private void SetMessage(ApiResponse result)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
    }
}
