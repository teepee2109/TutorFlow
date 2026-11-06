using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

public partial class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int TuitionId { get; set; }

    [Column(TypeName = "decimal(12, 0)")]
    public decimal AmountPaid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime PaymentDate { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    public int? ReceivedBy { get; set; }

    [StringLength(50)]
    public string? ReceiptNumber { get; set; }

    [ForeignKey("ReceivedBy")]
    [InverseProperty("Payments")]
    public virtual User? ReceivedByNavigation { get; set; }

    [ForeignKey("TuitionId")]
    [InverseProperty("Payments")]
    public virtual Tuition Tuition { get; set; } = null!;
}
