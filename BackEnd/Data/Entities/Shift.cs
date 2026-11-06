using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

public partial class Shift
{
    [Key]
    public int ShiftId { get; set; }

    [StringLength(50)]
    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    [InverseProperty("Shift")]
    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    [InverseProperty("Shift")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [InverseProperty("CurrentShift")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequestCurrentShifts { get; set; } = new List<RoomChangeRequest>();

    [InverseProperty("RequestedShift")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequestRequestedShifts { get; set; } = new List<RoomChangeRequest>();
}
