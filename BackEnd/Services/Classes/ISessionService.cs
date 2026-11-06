using BackEnd.Services.Common;

namespace BackEnd.Services.Classes;

public interface ISessionService
{
    Task<Result> GenerateForClassAsync(int classId);
    Task<Result> RescheduleSessionAsync(int sessionId, Dtos.Classes.SessionRescheduleDto request);
    Task<Result> CancelSessionAsync(int sessionId);
    Task<Result> AssignSubstituteAsync(int sessionId, int lecturerId);
}
