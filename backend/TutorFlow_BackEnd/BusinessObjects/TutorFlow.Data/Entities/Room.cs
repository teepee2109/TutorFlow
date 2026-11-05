using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Room
{
    public int RoomId { get; set; }

    public int FacilityId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int? Capacity { get; set; }

    public string Condition { get; set; } = null!;

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual Facility Facility { get; set; } = null!;

    public virtual ICollection<RoomChangeRequest> RoomChangeRequestCurrentRooms { get; set; } = new List<RoomChangeRequest>();

    public virtual ICollection<RoomChangeRequest> RoomChangeRequestRequestedRooms { get; set; } = new List<RoomChangeRequest>();
}
