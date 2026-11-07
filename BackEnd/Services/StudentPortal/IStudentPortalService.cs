using BackEnd.Dtos;
using BackEnd.Services.Common;

namespace BackEnd.Services.StudentPortal;

public interface IStudentPortalService
{
    Task<Result<List<StudentPortalScheduleItemDto>>> GetMyScheduleAsync(int studentId, DateOnly from, DateOnly to);
    Task<Result<List<StudentPortalScheduleItemDto>>> GetMyAttendanceAsync(int studentId, DateOnly from, DateOnly to);
    Task<Result<List<TuitionRecordDto>>> GetMyTuitionAsync(int studentId);
}
