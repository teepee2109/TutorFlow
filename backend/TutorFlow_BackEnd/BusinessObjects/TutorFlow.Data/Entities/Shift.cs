using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Shift
{
    public int ShiftId { get; set; }

    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<RoomChangeRequest> RoomChangeRequestCurrentShifts { get; set; } = new List<RoomChangeRequest>();

    public virtual ICollection<RoomChangeRequest> RoomChangeRequestRequestedShifts { get; set; } = new List<RoomChangeRequest>();
}
