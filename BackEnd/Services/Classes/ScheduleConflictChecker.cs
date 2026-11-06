using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Dtos.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Classes;

public sealed class ScheduleConflictChecker : IScheduleConflictChecker
{
    private readonly TutorFlowDbContext _db;

    public ScheduleConflictChecker(TutorFlowDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ScheduleConflictDto>>> PreviewClassConflictsAsync(
        ClassWriteDto request,
        int? excludeClassId = null)
    {
        if (!ClassScheduleDates.TryCreate(request, out var dates, out var error))
        {
            return Result<List<ScheduleConflictDto>>.Fail(ScheduleError.ValidationError, error);
        }

        var roomMatchesFacility = await _db.Rooms.AsNoTracking()
            .AnyAsync(room => room.RoomId == request.RoomId && room.FacilityId == request.FacilityId);
        if (!roomMatchesFacility)
        {
            return Result<List<ScheduleConflictDto>>.Fail(ScheduleError.ValidationError,
                "Phòng đã chọn không thuộc cơ sở của lớp.");
        }

        return await PreviewDatesAsync(request.RoomId, request.ShiftId, request.LecturerId, dates, excludeClassId);
    }

    public async Task<Result<List<ScheduleConflictDto>>> PreviewDatesAsync(
        int roomId,
        int shiftId,
        int lecturerId,
        IReadOnlyCollection<DateOnly> dates,
        int? excludeClassId = null,
        IReadOnlyCollection<int>? ignoreSessionIds = null)
    {
        if (dates.Count == 0)
        {
            return Result<List<ScheduleConflictDto>>.Fail(ScheduleError.ValidationError,
                "Không có buổi học nào để kiểm tra lịch.");
        }

        var room = await _db.Rooms.AsNoTracking()
            .Where(item => item.RoomId == roomId)
            .Select(item => new { item.RoomId, item.Condition })
            .FirstOrDefaultAsync();
        if (room == null)
        {
            return Result<List<ScheduleConflictDto>>.Fail(ScheduleError.NotFound, "Không tìm thấy phòng học.");
        }

        var distinctDates = dates.Distinct().OrderBy(date => date).ToArray();
        if (room.Condition == "Closed")
        {
            return Result<List<ScheduleConflictDto>>.Ok(distinctDates.Select(date => new ScheduleConflictDto
            {
                Date = date,
                Error = ScheduleError.RoomClosed,
                ConflictType = "Phòng đóng bảo trì",
                Message = "Phòng đang đóng bảo trì, không thể xếp lịch."
            }).ToList());
        }

        var query = _db.ClassSessions.AsNoTracking()
            .Where(session => distinctDates.Contains(session.SessionDate)
                && session.ShiftId == shiftId
                && session.Status != "Cancelled");

        if (excludeClassId.HasValue)
        {
            query = query.Where(session => session.ClassId != excludeClassId.Value);
        }

        if (ignoreSessionIds is { Count: > 0 })
        {
            var ignoredIds = ignoreSessionIds.Distinct().ToArray();
            query = query.Where(session => !ignoredIds.Contains(session.SessionId));
        }

        var occupied = await query
            .Where(session => session.RoomId == roomId || session.LecturerId == lecturerId)
            .Select(session => new
            {
                session.SessionDate,
                session.RoomId,
                session.LecturerId,
                session.Class.ClassName
            })
            .ToListAsync();

        var conflicts = new List<ScheduleConflictDto>();
        foreach (var date in distinctDates)
        {
            foreach (var session in occupied.Where(item => item.SessionDate == date))
            {
                if (session.RoomId == roomId)
                {
                    conflicts.Add(new ScheduleConflictDto
                    {
                        Date = date,
                        Error = ScheduleError.RoomConflict,
                        ConflictType = "Trùng phòng",
                        ConflictingClassName = session.ClassName,
                        Message = $"Phòng đã được lớp '{session.ClassName}' sử dụng trong ca này."
                    });
                }

                if (session.LecturerId == lecturerId)
                {
                    conflicts.Add(new ScheduleConflictDto
                    {
                        Date = date,
                        Error = ScheduleError.LecturerConflict,
                        ConflictType = "Trùng giảng viên",
                        ConflictingClassName = session.ClassName,
                        Message = $"Giảng viên đang dạy lớp '{session.ClassName}' trong ca này."
                    });
                }
            }
        }

        return Result<List<ScheduleConflictDto>>.Ok(conflicts);
    }
}
