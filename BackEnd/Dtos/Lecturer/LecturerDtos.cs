using System.ComponentModel.DataAnnotations;
using BackEnd.Dtos.Rooms;

namespace BackEnd.Dtos.Lecturer;

public sealed class LecturerScheduleSessionDto
{
    public int SessionId { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly SessionDate { get; init; }
    public int ShiftId { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string FacilityName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class AttendanceRosterDto
{
    public int SessionId { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly SessionDate { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<AttendanceRosterStudentDto> Students { get; init; } = new();
}

public sealed class AttendanceRosterStudentDto
{
    public int StudentId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AttendanceStatus { get; init; }
    public string? Note { get; init; }
}

public sealed class AttendanceSaveDto
{
    public List<AttendanceSaveItemDto> Items { get; set; } = new();
}

public sealed class AttendanceSaveItemDto
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required]
    [RegularExpression("^(Present|ExcusedAbsent|UnexcusedAbsent)$")]
    public string Status { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Note { get; set; }
}

public sealed class LecturerChangeRequestCreateDto
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    public int? SessionId { get; set; }
    public int? RequestedRoomId { get; set; }
    public int? RequestedShiftId { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class RoomChangeScopeOptionDto
{
    public string ScopeType { get; init; } = string.Empty;
    public int ScopeId { get; init; }
    public int ClassId { get; init; }
    public int? SessionId { get; init; }
    public string Key => $"{ScopeType}:{ScopeId}";
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class ShiftOptionDto
{
    public int ShiftId { get; init; }
    public string ShiftName { get; init; } = string.Empty;
}

public sealed class LecturerChangeRequestOptionsDto
{
    public List<RoomChangeScopeOptionDto> Scopes { get; init; } = new();
    public List<RoomOptionDto> Rooms { get; init; } = new();
    public List<ShiftOptionDto> Shifts { get; init; } = new();
}

public sealed class LecturerRoomChangeRequestDto
{
    public int RequestId { get; init; }
    public int ClassId { get; init; }
    public int? SessionId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public DateOnly? SessionDate { get; init; }
    public string CurrentRoom { get; init; } = string.Empty;
    public string? RequestedRoom { get; init; }
    public string CurrentShift { get; init; } = string.Empty;
    public string? RequestedShift { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewNote { get; init; }
}
