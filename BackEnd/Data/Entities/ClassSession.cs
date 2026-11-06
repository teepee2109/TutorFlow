using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("ClassId", "SessionDate", Name = "IX_Sessions_ClassDate")]
[Index("ClassId", "SessionDate", Name = "UQ_Session", IsUnique = true)]
public partial class ClassSession
{
    [Key]
    public int SessionId { get; set; }

    public int ClassId { get; set; }

    public DateOnly SessionDate { get; set; }

    public int RoomId { get; set; }

    public int ShiftId { get; set; }

    public int LecturerId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [InverseProperty("Session")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [ForeignKey("ClassId")]
    [InverseProperty("ClassSessions")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("LecturerId")]
    [InverseProperty("ClassSessions")]
    public virtual User Lecturer { get; set; } = null!;

    [ForeignKey("RoomId")]
    [InverseProperty("ClassSessions")]
    public virtual Room Room { get; set; } = null!;

    [InverseProperty("Session")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequests { get; set; } = new List<RoomChangeRequest>();

    [ForeignKey("ShiftId")]
    [InverseProperty("ClassSessions")]
    public virtual Shift Shift { get; set; } = null!;
}
