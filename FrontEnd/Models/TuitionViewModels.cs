using System.ComponentModel.DataAnnotations;

namespace FrontEnd.Models;

public sealed class TuitionPreviewItemViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int UnexcusedCount { get; set; }
    public int BillableSessions { get; set; }
    public int TotalSessions { get; set; }
    public decimal AmountPerSession { get; set; }
    public decimal TuitionAmount { get; set; }
    public bool AlreadyIssued { get; set; }
}

public sealed class TuitionPaymentViewModel
{
    public int PaymentId { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? ReceivedByName { get; set; }
}

public sealed class TuitionRecordViewModel
{
    public int TuitionId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public int BillableSessions { get; set; }
    public decimal AmountPerSession { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public List<TuitionPaymentViewModel> Payments { get; set; } = new();
    public List<PaymentRequestViewModel> PaymentRequests { get; set; } = new();
}

public sealed class PaymentRequestViewModel
{
    public int PaymentRequestId { get; set; }
    public int TuitionId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public decimal AmountRequested { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransferReference { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public string? ReviewedByName { get; set; }
}

public sealed class CreatePaymentRequestViewModel
{
    public int TuitionId { get; set; }

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Số tiền phải lớn hơn 0.")]
    public decimal AmountRequested { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TransferReference { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }
}

public sealed class ReviewPaymentRequestViewModel
{
    public int PaymentRequestId { get; set; }
    public string Decision { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ReviewNote { get; set; }
}

public sealed class TuitionIssueResultViewModel
{
    public int IssuedCount { get; set; }
    public int SkippedExistingCount { get; set; }
    public int SkippedNoBillableSessionsCount { get; set;}
}

public sealed class TuitionIssueRequestViewModel
{
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public int? ClassId { get; set; }
}

public sealed class RecordPaymentViewModel
{
    public int TuitionId { get; set; }

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Số tiền phải lớn hơn 0.")]
    public decimal AmountPaid { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ReceiptNumber { get; set; }
}

public sealed class TuitionsPageViewModel
{
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public List<TuitionPreviewItemViewModel> Preview { get; set; } = new();
    public List<TuitionRecordViewModel> Records { get; set; } = new();
    public List<PaymentRequestViewModel> PendingPaymentRequests { get; set; } = new();
}

public sealed class StudentPortalScheduleItemViewModel
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public string SessionStatus { get; set; } = string.Empty;
    public string? AttendanceStatus { get; set; }
    public string? AttendanceNote { get; set; }
}

public sealed class StudentSchedulePageViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<StudentPortalScheduleItemViewModel> Sessions { get; set; } = new();
}

public sealed class StudentTuitionPageViewModel
{
    public List<TuitionRecordViewModel> Records { get; set; } = new();
}
