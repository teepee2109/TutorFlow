using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Index("Status", Name = "IX_RCR_Status")]
public partial class RoomChangeRequest
{
    [Key]
    public int RequestId { get; set; }

    public int LecturerId { get; set; }

    public int ClassId { get; set; }

    public int? SessionId { get; set; }

    public int CurrentRoomId { get; set; }

    public int? RequestedRoomId { get; set; }

    public int CurrentShiftId { get; set; }

    public int? RequestedShiftId { get; set; }

    [StringLength(500)]
    public string Reason { get; set; } = null!;

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ReviewedAt { get; set; }

    [StringLength(255)]
    public string? ReviewNote { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("RoomChangeRequests")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("CurrentRoomId")]
    [InverseProperty("RoomChangeRequestCurrentRooms")]
    public virtual Room CurrentRoom { get; set; } = null!;

    [ForeignKey("CurrentShiftId")]
    [InverseProperty("RoomChangeRequestCurrentShifts")]
    public virtual Shift CurrentShift { get; set; } = null!;

    [ForeignKey("LecturerId")]
    [InverseProperty("RoomChangeRequestLecturers")]
    public virtual User Lecturer { get; set; } = null!;

    [ForeignKey("RequestedRoomId")]
    [InverseProperty("RoomChangeRequestRequestedRooms")]
    public virtual Room? RequestedRoom { get; set; }

    [ForeignKey("RequestedShiftId")]
    [InverseProperty("RoomChangeRequestRequestedShifts")]
    public virtual Shift? RequestedShift { get; set; }

    [ForeignKey("ReviewedBy")]
    [InverseProperty("RoomChangeRequestReviewedByNavigations")]
    public virtual User? ReviewedByNavigation { get; set; }

    [ForeignKey("SessionId")]
    [InverseProperty("RoomChangeRequests")]
    public virtual ClassSession? Session { get; set; }
}
