using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("StudentId", "PaymentStatus", Name = "IX_Tuitions_StudentStatus")]
[Index("StudentId", "ClassId", "PeriodFrom", "PeriodTo", Name = "UQ_Tuitions_Period", IsUnique = true)]
public partial class Tuition
{
    [Key]
    public int TuitionId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public DateOnly PeriodFrom { get; set; }

    public DateOnly PeriodTo { get; set; }

    public int BillableSessions { get; set; }

    [Column(TypeName = "decimal(12, 0)")]
    public decimal AmountPerSession { get; set; }

    [Column(TypeName = "decimal(23, 0)")]
    public decimal? TotalAmount { get; set; }

    [StringLength(20)]
    public string PaymentStatus { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("Tuitions")]
    public virtual Class Class { get; set; } = null!;

    [InverseProperty("Tuition")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();

    [ForeignKey("StudentId")]
    [InverseProperty("Tuitions")]
    public virtual User Student { get; set; } = null!;
}
