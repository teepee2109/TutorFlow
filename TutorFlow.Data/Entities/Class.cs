using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Class
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public int SubjectId { get; set; }

    public int LecturerId { get; set; }

    public int FacilityId { get; set; }

    public int RoomId { get; set; }

    public int ShiftId { get; set; }

    public string DaysOfWeek { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual Facility Facility { get; set; } = null!;

    public virtual User Lecturer { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<RoomChangeRequest> RoomChangeRequests { get; set; } = new List<RoomChangeRequest>();

    public virtual Shift Shift { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual ICollection<Tuition> Tuitions { get; set; } = new List<Tuition>();
}
