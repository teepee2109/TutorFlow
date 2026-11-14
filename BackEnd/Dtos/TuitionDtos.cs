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
    public int BillableSessions { get; init; }
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
    public int BillableSessions { get; init; }
    public decimal AmountPerSession { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal RemainingAmount { get; init; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateOnly? DueDate { get; init; }
    public List<TuitionPaymentDto> Payments { get; init; } = new();
    public List<PaymentRequestDto> PaymentRequests { get; init; } = new();
}

public sealed class PaymentRequestDto
{
    public int PaymentRequestId { get; init; }
    public int TuitionId { get; init; }
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public DateOnly PeriodFrom { get; init; }
    public DateOnly PeriodTo { get; init; }
    public decimal AmountRequested { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string? TransferReference { get; init; }
    public string? Note { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewNote { get; init; }
    public string? ReviewedByName { get; init; }
}

public sealed class PaymentRequestCreateDto
{
    [Range(typeof(decimal), "1", "999999999999")]
    public decimal AmountRequested { get; set; }

    [Required, StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TransferReference { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }
}

public sealed class PaymentRequestReviewDto
{
    [Required, RegularExpression("^(Approve|Reject)$")]
    public string Decision { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ReviewNote { get; set; }
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
    public int SkippedNoBillableSessionsCount { get; init; }
}

public sealed class PaymentCreateDto
{
    [Range(typeof(decimal), "1", "999999999999")]
    public decimal AmountPaid { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

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
