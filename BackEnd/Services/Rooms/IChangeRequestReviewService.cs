using BackEnd.Dtos.Rooms;
using BackEnd.Services.Common;

namespace BackEnd.Services.Rooms;

public interface IChangeRequestReviewService
{
    Task<Result<List<RoomChangeReviewDto>>> ListPendingAsync();
    Task<Result> ApproveAsync(int requestId, string? reviewNote);
    Task<Result> RejectAsync(int requestId, string reviewNote);
}
