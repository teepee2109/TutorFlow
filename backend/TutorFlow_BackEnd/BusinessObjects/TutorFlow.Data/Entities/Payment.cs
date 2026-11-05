using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int TuitionId { get; set; }

    public decimal AmountPaid { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? PaymentMethod { get; set; }

    public int? ReceivedBy { get; set; }

    public string? ReceiptNumber { get; set; }

    public virtual User? ReceivedByNavigation { get; set; }

    public virtual Tuition Tuition { get; set; } = null!;
}
