using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos;
using BackEnd.Dtos.Rooms;
using BackEnd.Dtos.Classes;
using BackEnd.Services.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Rooms;

public sealed class RoomService : IRoomService
{
    private readonly TutorFlowDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISessionService _sessionService;

    public RoomService(TutorFlowDbContext db, ICurrentUser currentUser, ISessionService sessionService)
    {
        _db = db;
        _currentUser = currentUser;
        _sessionService = sessionService;
    }

    public async Task<Result<List<RoomShiftStatusDto>>> GetStatusGridAsync(int? facilityId, DateOnly date)
    {
        if (date == default)
        {
            return Result<List<RoomShiftStatusDto>>.Fail(ScheduleError.ValidationError, "Ngày xem trạng thái phòng không hợp lệ.");
        }

        if (_currentUser.IsInRole("FacilityManager"))
        {
            if (!_currentUser.FacilityId.HasValue)
            {
                return Result<List<RoomShiftStatusDto>>.Fail(ScheduleError.InvalidRole, "Tài khoản Facility Manager chưa được gán cơ sở.");
            }
            facilityId = _currentUser.FacilityId;
        }
        else if (facilityId.HasValue && !await _db.Facilities.AnyAsync(item => item.FacilityId == facilityId.Value))
        {
            return Result<List<RoomShiftStatusDto>>.Fail(ScheduleError.NotFound, "Không tìm thấy cơ sở.");
        }

        try
        {
            var query = _db.RoomShiftStatuses
                .FromSqlInterpolated($"SELECT * FROM dbo.fn_RoomShiftStatus({date})")
                .AsNoTracking();
            if (facilityId.HasValue)
            {
                query = query.Where(slot => slot.FacilityId == facilityId.Value);
            }

            var slots = await query.OrderBy(slot => slot.FacilityName)
                .ThenBy(slot => slot.RoomNumber).ThenBy(slot => slot.ShiftId).ToListAsync();
            return Result<List<RoomShiftStatusDto>>.Ok(slots);
        }
        catch (Exception exception)
        {
            var mapped = SqlExceptionMapper.Map(exception);
            return Result<List<RoomShiftStatusDto>>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể tải lưới trạng thái phòng.");
        }
    }

    public async Task<Result<List<RoomOptionDto>>> FindEmptyRoomsAsync(DateOnly date, int shiftId, int? facilityId = null)
    {
        if (!_currentUser.IsInRole("FacilityManager"))
        {
            return Result<List<RoomOptionDto>>.Fail(ScheduleError.InvalidRole, "Chỉ Facility Manager được tìm phòng để xếp lịch.");
        }
        if (!_currentUser.FacilityId.HasValue)
        {
            return Result<List<RoomOptionDto>>.Fail(ScheduleError.InvalidRole, "Tài khoản Facility Manager chưa được gán cơ sở.");
        }
        if (date == default)
        {
            return Result<List<RoomOptionDto>>.Fail(ScheduleError.ValidationError, "Ngày tìm phòng không hợp lệ.");
        }
        if (!await _db.Shifts.AnyAsync(item => item.ShiftId == shiftId))
        {
            return Result<List<RoomOptionDto>>.Fail(ScheduleError.NotFound, "Không tìm thấy ca học.");
        }

        // FM chỉ được xếp vào phòng thuộc cơ sở mình phụ trách.
        facilityId = _currentUser.FacilityId;
        return Result<List<RoomOptionDto>>.Ok(await QueryEmptyRoomsAsync(date, shiftId, facilityId));
    }

    public async Task<Result<List<AffectedSessionDto>>> GetAffectedSessionsAsync(int roomId)
    {
        if (!_currentUser.IsInRole("FacilityManager") || !_currentUser.FacilityId.HasValue)
        {
            return Result<List<AffectedSessionDto>>.Fail(ScheduleError.InvalidRole, "Chỉ Facility Manager được xem các buổi ảnh hưởng của phòng.");
        }

        var room = await _db.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoomId == roomId && item.FacilityId == _currentUser.FacilityId.Value);
        if (room == null)
        {
            return Result<List<AffectedSessionDto>>.Fail(ScheduleError.NotFound, "Không tìm thấy phòng trong cơ sở được phân công.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var sessions = await _db.ClassSessions.AsNoTracking()
            .Where(item => item.RoomId == roomId && item.Status == "Scheduled" && item.SessionDate >= today)
            .Include(item => item.Class)
            .Include(item => item.Shift)
            .Include(item => item.Room)
            .Include(item => item.Lecturer)
            .OrderBy(item => item.SessionDate).ThenBy(item => item.ShiftId)
            .ToListAsync();

        var slotsByDateShift = new Dictionary<(DateOnly Date, int ShiftId), List<RoomOptionDto>>();
        var result = new List<AffectedSessionDto>();
        foreach (var session in sessions)
        {
            var key = (session.SessionDate, session.ShiftId);
            if (!slotsByDateShift.TryGetValue(key, out var availableRooms))
            {
                availableRooms = await QueryEmptyRoomsAsync(session.SessionDate, session.ShiftId, _currentUser.FacilityId);
                slotsByDateShift.Add(key, availableRooms);
            }

            result.Add(new AffectedSessionDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                ClassName = session.Class.ClassName,
                SessionDate = session.SessionDate,
                ShiftId = session.ShiftId,
                ShiftName = session.Shift.ShiftName,
                RoomId = session.RoomId,
                RoomNumber = session.Room.RoomNumber,
                LecturerName = session.Lecturer.FullName,
                AvailableRooms = availableRooms
            });
        }

        return Result<List<AffectedSessionDto>>.Ok(result);
    }

    public async Task<Result> SetConditionAsync(int roomId, string condition)
    {
        if (!_currentUser.IsInRole("FacilityManager") || !_currentUser.FacilityId.HasValue)
        {
            return Result.Fail(ScheduleError.InvalidRole, "Chỉ Facility Manager được thay đổi trạng thái phòng.");
        }
        if (condition is not ("Open" or "Closed"))
        {
            return Result.Fail(ScheduleError.ValidationError, "Trạng thái phòng chỉ được là Open hoặc Closed.");
        }

        var room = await _db.Rooms.FirstOrDefaultAsync(item =>
            item.RoomId == roomId && item.FacilityId == _currentUser.FacilityId.Value);
        if (room == null)
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy phòng trong cơ sở được phân công.");
        }
        if (room.Condition == condition)
        {
            return Result.Ok(condition == "Closed" ? "Phòng đã đóng bảo trì." : "Phòng đã mở.");
        }

        room.Condition = condition;
        try
        {
            await _db.SaveChangesAsync();
            return Result.Ok(condition == "Closed" ? "Đã đóng phòng bảo trì." : "Đã mở lại phòng.");
        }
        catch (Exception exception)
        {
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể cập nhật trạng thái phòng.");
        }
    }

    public async Task<Result> AssignSessionRoomAsync(int sessionId, int roomId, bool applyToWholeClass)
    {
        if (!_currentUser.IsInRole("FacilityManager") || !_currentUser.FacilityId.HasValue)
        {
            return Result.Fail(ScheduleError.InvalidRole, "Chỉ Facility Manager được xếp phòng.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var session = await _db.ClassSessions.AsNoTracking()
            .Include(item => item.Room)
            .Include(item => item.Class)
            .FirstOrDefaultAsync(item => item.SessionId == sessionId);
        if (session == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
        if (session.Status != "Scheduled" || session.SessionDate < today || session.Class.Status != "Active")
        {
            return Result.Fail(ScheduleError.ValidationError, "Chỉ có thể xếp lại phòng cho buổi Scheduled sắp tới của lớp đang mở.");
        }
        if (session.Room.FacilityId != _currentUser.FacilityId.Value)
        {
            return Result.Fail(ScheduleError.NotFound, "Buổi học không thuộc cơ sở được phân công.");
        }
        if (applyToWholeClass && session.Class.FacilityId != _currentUser.FacilityId.Value)
        {
            return Result.Fail(ScheduleError.ValidationError, "Chỉ được đổi phòng mặc định cho lớp thuộc cơ sở được phân công.");
        }

        var targetRoom = await _db.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoomId == roomId
                && item.FacilityId == _currentUser.FacilityId.Value
                && item.Condition == "Open");
        if (targetRoom == null)
        {
            return Result.Fail(ScheduleError.NotFound, "Phòng đích không tồn tại, đã đóng hoặc thuộc cơ sở khác.");
        }

        var emptyRooms = await QueryEmptyRoomsAsync(session.SessionDate, session.ShiftId, _currentUser.FacilityId);
        if (emptyRooms.All(item => item.RoomId != roomId))
        {
            return Result.Fail(ScheduleError.RoomConflict, "Phòng đích không còn trống trong ca và ngày đã chọn.");
        }

        return await _sessionService.RescheduleSessionAsync(sessionId, new SessionRescheduleDto
        {
            RoomId = roomId,
            ApplyToWholeClass = applyToWholeClass
        });
    }

    public async Task<Result> CancelAffectedSessionAsync(int sessionId)
    {
        if (!_currentUser.IsInRole("FacilityManager") || !_currentUser.FacilityId.HasValue)
        {
            return Result.Fail(ScheduleError.InvalidRole, "Chỉ Facility Manager được xử lý buổi bị ảnh hưởng.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var session = await _db.ClassSessions.AsNoTracking()
            .Include(item => item.Room)
            .FirstOrDefaultAsync(item => item.SessionId == sessionId);
        if (session == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
        if (session.Room.FacilityId != _currentUser.FacilityId.Value)
        {
            return Result.Fail(ScheduleError.NotFound, "Buổi học không thuộc cơ sở được phân công.");
        }
        if (session.Status != "Scheduled" || session.SessionDate < today)
        {
            return Result.Fail(ScheduleError.ValidationError, "Chỉ có thể hủy buổi Scheduled từ hôm nay trở đi.");
        }

        return await _sessionService.CancelSessionAsync(sessionId);
    }

    private async Task<List<RoomOptionDto>> QueryEmptyRoomsAsync(DateOnly date, int shiftId, int? facilityId)
    {
        var query = _db.RoomShiftStatuses
            .FromSqlInterpolated($"SELECT * FROM dbo.fn_RoomShiftStatus({date})")
            .AsNoTracking()
            .Where(slot => slot.ShiftId == shiftId
                && slot.SlotStatus == "Empty"
                && slot.RoomCondition == "Open");
        if (facilityId.HasValue)
        {
            query = query.Where(slot => slot.FacilityId == facilityId.Value);
        }

        return await query.OrderBy(slot => slot.RoomNumber)
            .Select(slot => new RoomOptionDto
            {
                RoomId = slot.RoomId,
                FacilityId = slot.FacilityId,
                FacilityName = slot.FacilityName,
                RoomNumber = slot.RoomNumber,
                Capacity = slot.Capacity
            }).ToListAsync();
    }
}
