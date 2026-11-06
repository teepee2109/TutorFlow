using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("SubjectName", Name = "UQ__Subjects__4C5A7D55E8E529BA", IsUnique = true)]
public partial class Subject
{
    [Key]
    public int SubjectId { get; set; }

    [StringLength(50)]
    public string SubjectName { get; set; } = null!;

    [InverseProperty("Subject")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
