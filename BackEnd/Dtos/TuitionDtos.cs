using System.ComponentModel.DataAnnotations;

namespace BackEnd.Dtos;

public sealed class TuitionPreviewItemDto
{
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public int PresentCount { get; init; }
    public int ExcusedCount { get; init; }
    public int UnexcusedCount { get; init; }
    public int TotalSessions { get; init; }
    public decimal AmountPerSession { get; init; }
    public decimal TuitionAmount { get; init; }
    public bool AlreadyIssued { get; init; }
}

public sealed class TuitionPaymentDto
{
    public int PaymentId { get; init; }
    public decimal AmountPaid { get; init; }
    public DateTime PaymentDate { get; init; }
    public string? PaymentMethod { get; init; }
    public string? ReceiptNumber { get; init; }
    public string? ReceivedByName { get; init; }
}

public sealed class TuitionRecordDto
{
    public int TuitionId { get; init; }
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly PeriodFrom { get; init; }
    public DateOnly PeriodTo { get; init; }
    public int SessionsAttended { get; init; }
    public decimal AmountPerSession { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal RemainingAmount { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public DateOnly? DueDate { get; init; }
    public List<TuitionPaymentDto> Payments { get; init; } = new();
}

public sealed class TuitionIssueRequestDto
{
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public int? ClassId { get; set; }
}

public sealed class TuitionIssueResultDto
{
    public int IssuedCount { get; init; }
    public int SkippedExistingCount { get; init; }
    public int SkippedNoAttendanceCount { get; init; }
}

public sealed class PaymentCreateDto
{
    [Range(typeof(decimal), "1", "999999999999")]
    public decimal AmountPaid { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [StringLength(50)]
    public string? ReceiptNumber { get; set; }
}

public sealed class StudentPortalScheduleItemDto
{
    public int SessionId { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly SessionDate { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string FacilityName { get; init; } = string.Empty;
    public string LecturerName { get; init; } = string.Empty;
    public string SessionStatus { get; init; } = string.Empty;
    public string? AttendanceStatus { get; init; }
    public string? AttendanceNote { get; init; }
}
