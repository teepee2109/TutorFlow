using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class RoomChangeRequest
{
    public int RequestId { get; set; }

    public int LecturerId { get; set; }

    public int ClassId { get; set; }

    public int? SessionId { get; set; }

    public int CurrentRoomId { get; set; }

    public int? RequestedRoomId { get; set; }

    public int CurrentShiftId { get; set; }

    public int? RequestedShiftId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Room CurrentRoom { get; set; } = null!;

    public virtual Shift CurrentShift { get; set; } = null!;

    public virtual User Lecturer { get; set; } = null!;

    public virtual Room? RequestedRoom { get; set; }

    public virtual Shift? RequestedShift { get; set; }

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual ClassSession? Session { get; set; }
}
