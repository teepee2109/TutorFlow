using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Table("Attendance")]
[Index("StudentId", Name = "IX_Attendance_StudentId")]
[Index("SessionId", "StudentId", Name = "UQ_Attendance", IsUnique = true)]
public partial class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }

    public int StudentId { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [StringLength(255)]
    public string? Note { get; set; }

    public int RecordedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime RecordedAt { get; set; }

    public int? UpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("RecordedBy")]
    [InverseProperty("AttendanceRecordedByNavigations")]
    public virtual User RecordedByNavigation { get; set; } = null!;

    [ForeignKey("SessionId")]
    [InverseProperty("Attendances")]
    public virtual ClassSession Session { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("AttendanceStudents")]
    public virtual User Student { get; set; } = null!;

    [ForeignKey("UpdatedBy")]
    [InverseProperty("AttendanceUpdatedByNavigations")]
    public virtual User? UpdatedByNavigation { get; set; }
}
