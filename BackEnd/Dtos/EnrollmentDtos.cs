using System.ComponentModel.DataAnnotations;

namespace BackEnd.Dtos;

public sealed class EnrollmentStudentOptionDto
{
    public int StudentId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class EnrollmentClassOptionDto
{
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string LecturerName { get; init; } = string.Empty;
}

public sealed class EnrollmentOptionsDto
{
    public List<EnrollmentStudentOptionDto> Students { get; init; } = new();
    public List<EnrollmentClassOptionDto> Classes { get; init; } = new();
}

public sealed class EnrollmentDto
{
    public int EnrollmentId { get; init; }
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly EnrollDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class EnrollmentRequestDto
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    public DateOnly EnrollDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public sealed class BulkEnrollmentRequestDto
{
    [MinLength(1, ErrorMessage = "Chọn ít nhất một học viên.")]
    [MaxLength(100, ErrorMessage = "Mỗi lần chỉ có thể xử lý tối đa 100 học viên.")]
    public List<int> StudentIds { get; set; } = new();

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp.")]
    public int ClassId { get; set; }

    public DateOnly EnrollDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public sealed class BulkEnrollmentStudentCheckDto
{
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public List<EnrollmentConflictDto> Conflicts { get; init; } = new();
}

public sealed class BulkEnrollmentCheckDto
{
    public List<BulkEnrollmentStudentCheckDto> Students { get; init; } = new();
    public int ReadyCount => Students.Count(item => item.Status == "Ready");
    public int AlreadyEnrolledCount => Students.Count(item => item.Status == "AlreadyEnrolled");
    public int ConflictCount => Students.Count(item => item.Status == "Conflict");
    public int FailedCount => Students.Count(item => item.Status == "Failed");
}

public sealed class BulkEnrollmentStudentResultDto
{
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public List<EnrollmentConflictDto> Conflicts { get; init; } = new();
}

public sealed class BulkEnrollmentResultDto
{
    public List<BulkEnrollmentStudentResultDto> Students { get; init; } = new();
    public int EnrolledCount => Students.Count(item => item.Status == "Enrolled");
    public int AlreadyEnrolledCount => Students.Count(item => item.Status == "AlreadyEnrolled");
    public int ConflictCount => Students.Count(item => item.Status == "Conflict");
    public int FailedCount => Students.Count(item => item.Status == "Failed");
}

public sealed class EnrollmentConflictDto
{
    public DateOnly Date { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public string ConflictingClassName { get; init; } = string.Empty;
}

public sealed class EnrollmentPreviewDto
{
    public bool AlreadyEnrolled { get; init; }
    public List<EnrollmentConflictDto> Conflicts { get; init; } = new();
    public bool CanEnroll => !AlreadyEnrolled && Conflicts.Count == 0;
}
