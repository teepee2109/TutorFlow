using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("ClassId", Name = "IX_Enrollments_ClassId")]
[Index("StudentId", "ClassId", Name = "UQ_Enrollment", IsUnique = true)]
public partial class Enrollment
{
    [Key]
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public DateOnly EnrollDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [ForeignKey("ClassId")]
    [InverseProperty("Enrollments")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("Enrollments")]
    public virtual User Student { get; set; } = null!;
}
