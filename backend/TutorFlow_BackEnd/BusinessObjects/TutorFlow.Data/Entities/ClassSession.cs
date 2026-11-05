using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class ClassSession
{
    public int SessionId { get; set; }

    public int ClassId { get; set; }

    public DateOnly SessionDate { get; set; }

    public int RoomId { get; set; }

    public int ShiftId { get; set; }

    public int LecturerId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual User Lecturer { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<RoomChangeRequest> RoomChangeRequests { get; set; } = new List<RoomChangeRequest>();

    public virtual Shift Shift { get; set; } = null!;
}
