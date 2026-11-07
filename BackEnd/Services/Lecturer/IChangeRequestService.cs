using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;

namespace BackEnd.Services.Lecturer;

public interface IChangeRequestService
{
    Task<Result<LecturerChangeRequestOptionsDto>> GetCreateOptionsAsync(int lecturerId);
    Task<Result<LecturerRoomChangeRequestDto>> CreateAsync(LecturerChangeRequestCreateDto request, int lecturerId);
    Task<Result<List<LecturerRoomChangeRequestDto>>> ListMineAsync(int lecturerId);
}
