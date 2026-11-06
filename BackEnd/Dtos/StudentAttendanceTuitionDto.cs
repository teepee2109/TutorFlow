namespace BackEnd.Dtos;

public class StudentAttendanceTuitionDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int UnexcusedCount { get; set; }
    public int TotalSessions { get; set; }
    public decimal TuitionAmount { get; set; }
}
