namespace BackEnd.Dtos;

public class RoomShiftStatusDto
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string RoomCondition { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string SlotStatus { get; set; } = string.Empty; // "Empty", "Occupied", "Closed for Maintenance"
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? LecturerId { get; set; }
    public string? LecturerName { get; set; }
    public int? SessionId { get; set; }
}
