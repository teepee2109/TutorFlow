using System.ComponentModel.DataAnnotations;

namespace FrontEnd.Models;

public sealed class RoomOptionViewModel
{
    public int RoomId { get; set; }
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public int? Capacity { get; set; }
}

public sealed class RoomGridSlotViewModel
{
    public RoomShiftStatusViewModel Slot { get; set; } = new();
    public List<RoomOptionViewModel> EmptyRooms { get; set; } = new();
    public bool ShowRoomConditionAction { get; set; }
}

public sealed class AffectedSessionViewModel
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public List<RoomOptionViewModel> AvailableRooms { get; set; } = new();
}

public sealed class RoomGridViewModel
{
    public DateOnly TargetDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool CanManageRooms { get; set; }
    public int? AffectedRoomId { get; set; }
    public string? AffectedRoomNumber { get; set; }
    public List<RoomGridSlotViewModel> Slots { get; set; } = new();
    public List<AffectedSessionViewModel> AffectedSessions { get; set; } = new();
}

public sealed class RoomChangeReviewViewModel
{
    public int RequestId { get; set; }
    public int ClassId { get; set; }
    public int? SessionId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateOnly? SessionDate { get; set; }
    public string CurrentRoom { get; set; } = string.Empty;
    public string? RequestedRoom { get; set; }
    public string CurrentShift { get; set; } = string.Empty;
    public string? RequestedShift { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class RoomChangeReviewListViewModel
{
    public List<RoomChangeReviewViewModel> Requests { get; set; } = new();
}

public sealed class RejectRoomChangeViewModel
{
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(255)]
    public string ReviewNote { get; set; } = string.Empty;
}
