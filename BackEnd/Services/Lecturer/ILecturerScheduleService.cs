using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;

namespace BackEnd.Services.Lecturer;

public interface ILecturerScheduleService
{
    Task<Result<List<LecturerScheduleSessionDto>>> GetMyScheduleAsync(int lecturerId, DateOnly from, DateOnly to);
}
