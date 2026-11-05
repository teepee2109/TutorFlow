using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }

    public int StudentId { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public int RecordedBy { get; set; }

    public DateTime RecordedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User RecordedByNavigation { get; set; } = null!;

    public virtual ClassSession Session { get; set; } = null!;

    public virtual User Student { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
