using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("LecturerId", Name = "IX_Classes_LecturerId")]
[Index("RoomId", "ShiftId", Name = "IX_Classes_RoomShift")]
public partial class Class
{
    [Key]
    public int ClassId { get; set; }

    [StringLength(100)]
    public string ClassName { get; set; } = null!;

    public int SubjectId { get; set; }

    public int LecturerId { get; set; }

    public int FacilityId { get; set; }

    public int RoomId { get; set; }

    public int ShiftId { get; set; }

    [StringLength(20)]
    public string DaysOfWeek { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public int? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("ClassCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    [ForeignKey("FacilityId")]
    [InverseProperty("Classes")]
    public virtual Facility Facility { get; set; } = null!;

    [ForeignKey("LecturerId")]
    [InverseProperty("ClassLecturers")]
    public virtual User Lecturer { get; set; } = null!;

    [ForeignKey("RoomId, FacilityId")]
    [InverseProperty("Classes")]
    public virtual Room Room { get; set; } = null!;

    [InverseProperty("Class")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequests { get; set; } = new List<RoomChangeRequest>();

    [ForeignKey("ShiftId")]
    [InverseProperty("Classes")]
    public virtual Shift Shift { get; set; } = null!;

    [ForeignKey("SubjectId")]
    [InverseProperty("Classes")]
    public virtual Subject Subject { get; set; } = null!;

    [InverseProperty("Class")]
    public virtual ICollection<Tuition> Tuitions { get; set; } = new List<Tuition>();
}
