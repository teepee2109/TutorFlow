using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Tuition
{
    public int TuitionId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public DateOnly PeriodFrom { get; set; }

    public DateOnly PeriodTo { get; set; }

    public int SessionsAttended { get; set; }

    public decimal AmountPerSession { get; set; }

    public decimal? TotalAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User Student { get; set; } = null!;
}
