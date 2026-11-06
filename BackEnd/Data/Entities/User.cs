using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("RoleId", Name = "IX_Users_RoleId")]
[Index("Email", Name = "UQ__Users__A9D10534E7BE51DF", IsUnique = true)]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("RecordedByNavigation")]
    public virtual ICollection<Attendance> AttendanceRecordedByNavigations { get; set; } = new List<Attendance>();

    [InverseProperty("Student")]
    public virtual ICollection<Attendance> AttendanceStudents { get; set; } = new List<Attendance>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Attendance> AttendanceUpdatedByNavigations { get; set; } = new List<Attendance>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Class> ClassCreatedByNavigations { get; set; } = new List<Class>();

    [InverseProperty("Lecturer")]
    public virtual ICollection<Class> ClassLecturers { get; set; } = new List<Class>();

    [InverseProperty("Lecturer")]
    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    [InverseProperty("Student")]
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    [InverseProperty("Manager")]
    public virtual Facility? Facility { get; set; }

    [InverseProperty("ReceivedByNavigation")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("Lecturer")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequestLecturers { get; set; } = new List<RoomChangeRequest>();

    [InverseProperty("ReviewedByNavigation")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequestReviewedByNavigations { get; set; } = new List<RoomChangeRequest>();

    [InverseProperty("Student")]
    public virtual ICollection<Tuition> Tuitions { get; set; } = new List<Tuition>();
}
