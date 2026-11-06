using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("FacilityId", "RoomNumber", Name = "UQ_Room_Facility", IsUnique = true)]
[Index("RoomId", "FacilityId", Name = "UQ_Room_Facility_Composite", IsUnique = true)]
public partial class Room
{
    [Key]
    public int RoomId { get; set; }

    public int FacilityId { get; set; }

    [StringLength(20)]
    public string RoomNumber { get; set; } = null!;

    public int? Capacity { get; set; }

    [StringLength(20)]
    public string Condition { get; set; } = null!;

    [InverseProperty("Room")]
    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    [InverseProperty("Room")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [ForeignKey("FacilityId")]
    [InverseProperty("Rooms")]
    public virtual Facility Facility { get; set; } = null!;

    [InverseProperty("CurrentRoom")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequestCurrentRooms { get; set; } = new List<RoomChangeRequest>();

    [InverseProperty("RequestedRoom")]
    public virtual ICollection<RoomChangeRequest> RoomChangeRequestRequestedRooms { get; set; } = new List<RoomChangeRequest>();
}
