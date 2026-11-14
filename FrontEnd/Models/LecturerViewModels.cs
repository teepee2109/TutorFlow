using System.ComponentModel.DataAnnotations;

namespace FrontEnd.Models;

public sealed class LecturerScheduleSessionViewModel
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class LecturerScheduleViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<LecturerScheduleSessionViewModel> Sessions { get; set; } = new();
}

public sealed class AttendanceRosterStudentViewModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AttendanceStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class AttendanceRosterViewModel
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<AttendanceRosterStudentViewModel> Students { get; set; } = new();
}

public sealed class AttendanceSessionsPageViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<AttendanceSessionListViewModel> Sessions { get; set; } = new();
}

public sealed class AttendanceSessionListViewModel
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int EligibleStudentCount { get; set; }
    public int RecordedAttendanceCount { get; set; }
}

public sealed class AttendanceSaveStudentViewModel
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required]
    [RegularExpression("^(Present|ExcusedAbsent|UnexcusedAbsent)$")]
    public string AttendanceStatus { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Note { get; set; }
}

public sealed class AttendanceSaveViewModel
{
    public int SessionId { get; set; }
    public List<AttendanceSaveStudentViewModel> Students { get; set; } = new();
}

public sealed class RoomChangeScopeOptionViewModel
{
    public string ScopeType { get; set; } = string.Empty;
    public int ScopeId { get; set; }
    public int ClassId { get; set; }
    public int? SessionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class LecturerChangeRequestOptionsViewModel
{
    public List<RoomChangeScopeOptionViewModel> Scopes { get; set; } = new();
    public List<RoomOptionViewModel> Rooms { get; set; } = new();
    public List<ShiftOptionViewModel> Shifts { get; set; } = new();
}

public sealed class ShiftOptionViewModel
{
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
}

public sealed class CreateRoomChangeRequestViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn lớp hoặc buổi học.")]
    public string ScopeKey { get; set; } = string.Empty;

    public int? RequestedRoomId { get; set; }
    public int? RequestedShiftId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do.")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public LecturerChangeRequestOptionsViewModel Options { get; set; } = new();
}

public sealed class LecturerRoomChangeRequestViewModel
{
    public int RequestId { get; set; }
    public int ClassId { get; set; }
    public int? SessionId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateOnly? SessionDate { get; set; }
    public string CurrentRoom { get; set; } = string.Empty;
    public string? RequestedRoom { get; set; }
    public string CurrentShift { get; set; } = string.Empty;
    public string? RequestedShift { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public sealed class LecturerRoomChangeRequestListViewModel
{
    public List<LecturerRoomChangeRequestViewModel> Requests { get; set; } = new();
}
