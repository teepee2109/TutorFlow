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
