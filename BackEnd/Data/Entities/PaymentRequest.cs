using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index(nameof(TuitionId), nameof(Status), Name = "IX_PaymentRequests_TuitionStatus")]
[Index(nameof(PaymentId), Name = "UX_PaymentRequests_PaymentId", IsUnique = true)]
public partial class PaymentRequest
{
    [Key]
    public int PaymentRequestId { get; set; }

    public int TuitionId { get; set; }
    public int StudentId { get; set; }

    [Column(TypeName = "decimal(12, 0)")]
    public decimal AmountRequested { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TransferReference { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [Column(TypeName = "datetime")]
    public DateTime RequestedAt { get; set; }

    public int? ReviewedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ReviewedAt { get; set; }

    [StringLength(255)]
    public string? ReviewNote { get; set; }

    public int? PaymentId { get; set; }

    [ForeignKey(nameof(TuitionId))]
    public virtual Tuition Tuition { get; set; } = null!;

    [ForeignKey(nameof(StudentId))]
    public virtual User Student { get; set; } = null!;

    [ForeignKey(nameof(ReviewedBy))]
    public virtual User? ReviewedByNavigation { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public virtual Payment? Payment { get; set; }
}
