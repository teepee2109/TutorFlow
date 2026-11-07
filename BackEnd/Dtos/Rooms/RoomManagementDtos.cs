using System.ComponentModel.DataAnnotations;

namespace BackEnd.Dtos.Rooms;

public sealed class RoomConditionRequest
{
    [Required]
    [RegularExpression("^(Open|Closed)$")]
    public string Condition { get; set; } = string.Empty;
}

public sealed class RoomAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    public bool ApplyToWholeClass { get; set; }
}

public sealed class RoomOptionDto
{
    public int RoomId { get; init; }
    public int FacilityId { get; init; }
    public string FacilityName { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public int? Capacity { get; init; }
}

public sealed class AffectedSessionDto
{
    public int SessionId { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly SessionDate { get; init; }
    public int ShiftId { get; init; }
    public string ShiftName { get; init; } = string.Empty;
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string LecturerName { get; init; } = string.Empty;
    public List<RoomOptionDto> AvailableRooms { get; init; } = new();
}

public sealed class RoomChangeReviewDto
{
    public int RequestId { get; init; }
    public int ClassId { get; init; }
    public int? SessionId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string LecturerName { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public DateOnly? SessionDate { get; init; }
    public string CurrentRoom { get; init; } = string.Empty;
    public string? RequestedRoom { get; init; }
    public string CurrentShift { get; init; } = string.Empty;
    public string? RequestedShift { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class ReviewNoteRequest
{
    [StringLength(255)]
    public string? ReviewNote { get; set; }
}

public sealed class RejectRoomChangeRequest
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string ReviewNote { get; set; } = string.Empty;
}
