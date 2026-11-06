using BackEnd.Services.Common;
using System.ComponentModel.DataAnnotations;

namespace BackEnd.Dtos.Classes;

public sealed class ClassWriteDto
{
    [Required, StringLength(100)]
    public string ClassName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SubjectId { get; set; }

    [Range(1, int.MaxValue)]
    public int LecturerId { get; set; }

    [Range(1, int.MaxValue)]
    public int FacilityId { get; set; }

    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    [Range(1, int.MaxValue)]
    public int ShiftId { get; set; }

    [Required, StringLength(20)]
    public string DaysOfWeek { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public sealed class ChangeLecturerDto
{
    [Range(1, int.MaxValue)]
    public int LecturerId { get; set; }
}

public sealed class SessionRescheduleDto
{
    public int? RoomId { get; set; }
    public int? ShiftId { get; set; }
    public DateOnly? SessionDate { get; set; }
    public bool ApplyToWholeClass { get; set; }
}

public sealed class AssignSubstituteDto
{
    [Range(1, int.MaxValue)]
    public int LecturerId { get; set; }
}

public sealed class ClassLookupOptionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? FacilityId { get; init; }
    public int? Capacity { get; init; }
    public string? Condition { get; init; }
}

public sealed class ClassOptionsDto
{
    public List<ClassLookupOptionDto> Subjects { get; init; } = new();
    public List<ClassLookupOptionDto> Lecturers { get; init; } = new();
    public List<ClassLookupOptionDto> Facilities { get; init; } = new();
    public List<ClassLookupOptionDto> Rooms { get; init; } = new();
    public List<ClassLookupOptionDto> Shifts { get; init; } = new();
}

public class ClassListItemDto
{
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public int SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public int LecturerId { get; init; }
    public string LecturerName { get; init; } = string.Empty;
    public int FacilityId { get; init; }
    public string FacilityName { get; init; } = string.Empty;
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public int ShiftId { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public string DaysOfWeek { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public int SessionCount { get; init; }
}

public sealed class ClassDetailDto : ClassListItemDto
{
    public List<ClassSessionDto> Sessions { get; init; } = new();
}

public sealed class ClassSessionDto
{
    public int SessionId { get; init; }
    public DateOnly SessionDate { get; init; }
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public int ShiftId { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public int LecturerId { get; init; }
    public string LecturerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class ScheduleConflictDto
{
    public DateOnly Date { get; init; }
    public ScheduleError Error { get; init; }
    public string ConflictType { get; init; } = string.Empty;
    public string? ConflictingClassName { get; init; }
    public string Message { get; init; } = string.Empty;
}
