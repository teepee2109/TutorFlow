using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attendance> AttendanceRecordedByNavigations { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceStudents { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceUpdatedByNavigations { get; set; } = new List<Attendance>();

    public virtual ICollection<Class> ClassCreatedByNavigations { get; set; } = new List<Class>();

    public virtual ICollection<Class> ClassLecturers { get; set; } = new List<Class>();

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual Facility? Facility { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<RoomChangeRequest> RoomChangeRequestLecturers { get; set; } = new List<RoomChangeRequest>();

    public virtual ICollection<RoomChangeRequest> RoomChangeRequestReviewedByNavigations { get; set; } = new List<RoomChangeRequest>();

    public virtual ICollection<Tuition> Tuitions { get; set; } = new List<Tuition>();
}
