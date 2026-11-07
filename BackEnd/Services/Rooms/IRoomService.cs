using BackEnd.Dtos;
using BackEnd.Dtos.Rooms;
using BackEnd.Services.Common;

namespace BackEnd.Services.Rooms;

public interface IRoomService
{
    Task<Result<List<RoomShiftStatusDto>>> GetStatusGridAsync(int? facilityId, DateOnly date);
    Task<Result<List<RoomOptionDto>>> FindEmptyRoomsAsync(DateOnly date, int shiftId, int? facilityId = null);
    Task<Result<List<AffectedSessionDto>>> GetAffectedSessionsAsync(int roomId);
    Task<Result> SetConditionAsync(int roomId, string condition);
    Task<Result> AssignSessionRoomAsync(int sessionId, int roomId, bool applyToWholeClass);
    Task<Result> CancelAffectedSessionAsync(int sessionId);
}
