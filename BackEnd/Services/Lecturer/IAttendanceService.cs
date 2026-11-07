using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;

namespace BackEnd.Services.Lecturer;

public interface IAttendanceService
{
    Task<Result<AttendanceRosterDto>> GetRosterAsync(int sessionId);
    Task<Result> SaveAsync(int sessionId, IReadOnlyCollection<AttendanceSaveItemDto> items, int lecturerId);
    Task<Result> CompleteSessionAsync(int sessionId, int lecturerId);
}
