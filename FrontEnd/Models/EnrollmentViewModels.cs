using System.ComponentModel.DataAnnotations;

namespace FrontEnd.Models;

public sealed class EnrollmentStudentOptionViewModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class EnrollmentClassOptionViewModel
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
}

public sealed class EnrollmentOptionsViewModel
{
    public List<EnrollmentStudentOptionViewModel> Students { get; set; } = new();
    public List<EnrollmentClassOptionViewModel> Classes { get; set; } = new();
}

public sealed class EnrollmentViewModel
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class EnrollmentRequestViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học viên.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp.")]
    public int ClassId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly EnrollDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public sealed class EnrollmentConflictViewModel
{
    public DateOnly Date { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string ConflictingClassName { get; set; } = string.Empty;
}

public sealed class EnrollmentPreviewViewModel
{
    public bool AlreadyEnrolled { get; set; }
    public bool CanEnroll { get; set; }
    public List<EnrollmentConflictViewModel> Conflicts { get; set; } = new();
}

public sealed class EnrollmentsPageViewModel
{
    public EnrollmentOptionsViewModel Options { get; set; } = new();
    public EnrollmentRequestViewModel Form { get; set; } = new();
    public EnrollmentPreviewViewModel? Preview { get; set; }
    public List<EnrollmentViewModel> Enrollments { get; set; } = new();
    public string? Search { get; set; }
    public string Status { get; set; } = "All";
    public int? ClassFilterId { get; set; }
}
