using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public DateOnly EnrollDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
