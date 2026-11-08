using System.ComponentModel.DataAnnotations;

namespace FrontEnd.Models;

public sealed class ClassLookupOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? FacilityId { get; set; }
    public int? Capacity { get; set; }
    public string? Condition { get; set; }
}

public sealed class ClassOptionsViewModel
{
    public List<ClassLookupOptionViewModel> Subjects { get; set; } = new();
    public List<ClassLookupOptionViewModel> Lecturers { get; set; } = new();
    public List<ClassLookupOptionViewModel> Facilities { get; set; } = new();
    public List<ClassLookupOptionViewModel> Rooms { get; set; } = new();
    public List<ClassLookupOptionViewModel> Shifts { get; set; } = new();
}

public class ClassWriteViewModel
{
    public int? ClassId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên lớp.")]
    [StringLength(100)]
    public string ClassName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học.")]
    public int SubjectId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn giảng viên.")]
    public int LecturerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn cơ sở.")]
    public int FacilityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng.")]
    public int RoomId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn ca học.")]
    public int ShiftId { get; set; }

    [Required(ErrorMessage = "Nhập ngày học theo dạng 2,4,6 (Thứ 2 đến Chủ nhật: 2..8).")]
    [StringLength(20)]
    public string DaysOfWeek { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));

    public ClassOptionsViewModel Options { get; set; } = new();
    public List<ScheduleConflictViewModel> Conflicts { get; set; } = new();
}

public class ClassListItemViewModel
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int LecturerId { get; set; }
    public string LecturerName { get; set; } = string.Empty;
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string DaysOfWeek { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SessionCount { get; set; }
}

public sealed class ClassDetailViewModel : ClassListItemViewModel
{
    public List<ClassSessionViewModel> Sessions { get; set; } = new();
    public ClassOptionsViewModel Options { get; set; } = new();
}

public sealed class ClassSessionViewModel
{
    public int SessionId { get; set; }
    public DateOnly SessionDate { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public int LecturerId { get; set; }
    public string LecturerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class ScheduleConflictViewModel
{
    public DateOnly Date { get; set; }
    public int Error { get; set; }
    public string ConflictType { get; set; } = string.Empty;
    public string? ConflictingClassName { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class SessionRescheduleViewModel
{
    public int? RoomId { get; set; }
    public int? ShiftId { get; set; }
    public DateOnly? SessionDate { get; set; }
    public bool ApplyToWholeClass { get; set; }
}

public sealed class ClassesIndexViewModel
{
    public List<ClassListItemViewModel> Classes { get; set; } = new();
    public ClassOptionsViewModel Options { get; set; } = new();
    public string Search { get; set; } = string.Empty;
    public string Status { get; set; } = "All";
    public int? SubjectId { get; set; }
    public int? LecturerId { get; set; }
}
