using System.Data;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Dtos.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Classes;

public sealed class SessionService : ISessionService
{
    private readonly TutorFlowDbContext _db;
    private readonly IScheduleConflictChecker _conflictChecker;

    public SessionService(TutorFlowDbContext db, IScheduleConflictChecker conflictChecker)
    {
        _db = db;
        _conflictChecker = conflictChecker;
    }

    public async Task<Result> GenerateForClassAsync(int classId)
    {
        var classEntity = await _db.Classes.FirstOrDefaultAsync(item => item.ClassId == classId);
        if (classEntity == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy lớp học.");
        if (classEntity.Status != "Active") return Result.Fail(ScheduleError.ValidationError, "Không thể sinh buổi cho lớp đã đóng.");

        var request = ToScheduleRequest(classEntity);
        if (!ClassScheduleDates.TryCreate(request, out var allDates, out var dateError))
        {
            return Result.Fail(ScheduleError.ValidationError, dateError);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var existingDates = await _db.ClassSessions.AsNoTracking()
            .Where(session => session.ClassId == classId)
            .Select(session => session.SessionDate)
            .ToListAsync();
        var existingSet = existingDates.ToHashSet();
        var missingDates = allDates.Where(date => date >= today && !existingSet.Contains(date)).ToArray();
        if (missingDates.Length == 0)
        {
            return Result.Ok("Không có buổi học mới cần sinh.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var conflicts = await _conflictChecker.PreviewDatesAsync(
                classEntity.RoomId, classEntity.ShiftId, classEntity.LecturerId, missingDates, classId);
            if (!conflicts.Success || conflicts.Value == null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(conflicts.Error ?? ScheduleError.ValidationError, conflicts.Message);
            }
            if (conflicts.Value.Count > 0)
            {
                await transaction.RollbackAsync();
                return Result.Fail(conflicts.Value[0].Error, FormatConflicts(conflicts.Value));
            }

            _db.ClassSessions.AddRange(missingDates.Select(date => new Data.Entities.ClassSession
            {
                ClassId = classEntity.ClassId,
                SessionDate = date,
                RoomId = classEntity.RoomId,
                ShiftId = classEntity.ShiftId,
                LecturerId = classEntity.LecturerId,
                Status = "Scheduled"
            }));
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok($"Đã sinh {missingDates.Length} buổi học.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể sinh buổi học lúc này.");
        }
    }

    public async Task<Result> RescheduleSessionAsync(int sessionId, SessionRescheduleDto request)
    {
        var selectedSession = await _db.ClassSessions
            .Include(session => session.Class)
            .FirstOrDefaultAsync(session => session.SessionId == sessionId);
        if (selectedSession == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
        if (selectedSession.Status != "Scheduled")
        {
            return Result.Fail(ScheduleError.ValidationError, "Chỉ có thể dời buổi học đang ở trạng thái Scheduled.");
        }
        if (selectedSession.Class.Status != "Active")
        {
            return Result.Fail(ScheduleError.ValidationError, "Không thể dời lịch của lớp đã đóng.");
        }
        if (request.RoomId == null && request.ShiftId == null && request.SessionDate == null)
        {
            return Result.Fail(ScheduleError.ValidationError, "Hãy chọn ít nhất một thông tin cần thay đổi.");
        }

        var roomId = request.RoomId ?? selectedSession.RoomId;
        var shiftId = request.ShiftId ?? selectedSession.ShiftId;
        var room = await _db.Rooms.AsNoTracking()
            .Where(item => item.RoomId == roomId)
            .Select(item => new { item.RoomId, item.FacilityId, item.Condition })
            .FirstOrDefaultAsync();
        if (room == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy phòng học.");
        if (request.ApplyToWholeClass && room.FacilityId != selectedSession.Class.FacilityId)
        {
            return Result.Fail(ScheduleError.ValidationError, "Phòng mặc định của cả lớp phải thuộc cơ sở của lớp.");
        }
        if (room.Condition == "Closed") return Result.Fail(ScheduleError.RoomClosed, "Phòng đang đóng bảo trì.");
        if (!await _db.Shifts.AnyAsync(shift => shift.ShiftId == shiftId))
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy ca học.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var targetSessions = request.ApplyToWholeClass
            ? await _db.ClassSessions.Where(item => item.ClassId == selectedSession.ClassId
                    && item.Status == "Scheduled" && item.SessionDate >= today)
                .ToListAsync()
            : new List<Data.Entities.ClassSession> { selectedSession };
        if (targetSessions.Count == 0)
        {
            return Result.Fail(ScheduleError.ValidationError, "Lớp không còn buổi Scheduled sắp tới để dời.");
        }
        if (request.ApplyToWholeClass && selectedSession.SessionDate < today)
        {
            return Result.Fail(ScheduleError.ValidationError, "Chọn một buổi Scheduled sắp tới để dời lịch cả lớp.");
        }

        var deltaDays = request.ApplyToWholeClass && request.SessionDate.HasValue
            ? request.SessionDate.Value.DayNumber - selectedSession.SessionDate.DayNumber
            : 0;
        var plannedDates = new Dictionary<int, DateOnly>();
        try
        {
            foreach (var item in targetSessions)
            {
                var targetDate = request.ApplyToWholeClass
                    ? item.SessionDate.AddDays(deltaDays)
                    : request.SessionDate ?? item.SessionDate;
                if (targetDate < today)
                {
                    return Result.Fail(ScheduleError.ValidationError, "Không thể dời một buổi Scheduled về ngày đã qua.");
                }
                plannedDates[item.SessionId] = targetDate;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result.Fail(ScheduleError.ValidationError, "Ngày dời buổi học vượt phạm vi hợp lệ.");
        }

        var ignoredIds = plannedDates.Keys.ToArray();
        var plannedDateValues = plannedDates.Values.ToArray();
        var duplicateDate = await _db.ClassSessions.AsNoTracking().AnyAsync(item =>
            item.ClassId == selectedSession.ClassId
            && plannedDateValues.Contains(item.SessionDate)
            && !ignoredIds.Contains(item.SessionId));
        if (duplicateDate)
        {
            return Result.Fail(ScheduleError.SessionDuplicate, "Lớp đã có một buổi học khác trong ngày được chọn.");
        }

        // When called by ChangeRequestReviewService, join its transaction so the
        // schedule update and request review status commit or roll back together.
        await using var transaction = _db.Database.CurrentTransaction == null
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            var conflicts = new List<ScheduleConflictDto>();
            foreach (var group in targetSessions.GroupBy(item => item.LecturerId))
            {
                var result = await _conflictChecker.PreviewDatesAsync(
                    roomId, shiftId, group.Key,
                    group.Select(item => plannedDates[item.SessionId]).ToArray(),
                    ignoreSessionIds: ignoredIds);
                if (!result.Success || result.Value == null)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return Result.Fail(result.Error ?? ScheduleError.ValidationError, result.Message);
                }
                conflicts.AddRange(result.Value);
            }

            if (conflicts.Count > 0)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return Result.Fail(conflicts[0].Error, FormatConflicts(conflicts));
            }

            foreach (var item in targetSessions)
            {
                item.RoomId = roomId;
                item.ShiftId = shiftId;
                item.SessionDate = plannedDates[item.SessionId];
            }

            if (request.ApplyToWholeClass)
            {
                selectedSession.Class.RoomId = roomId;
                selectedSession.Class.ShiftId = shiftId;
                if (deltaDays != 0)
                {
                    selectedSession.Class.StartDate = selectedSession.Class.StartDate.AddDays(deltaDays);
                    selectedSession.Class.EndDate = selectedSession.Class.EndDate.AddDays(deltaDays);
                    selectedSession.Class.DaysOfWeek = ClassScheduleDates.ShiftWeekDays(selectedSession.Class.DaysOfWeek, deltaDays);
                }
            }

            await _db.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            return Result.Ok(request.ApplyToWholeClass
                ? $"Đã dời {targetSessions.Count} buổi học của lớp."
                : "Đã dời buổi học.");
        }
        catch (Exception exception)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
                _db.ChangeTracker.Clear();
            }
            return FailureFromException(exception, "Không thể dời buổi học lúc này.");
        }
    }

    public async Task<Result> CancelSessionAsync(int sessionId)
    {
        var session = await _db.ClassSessions.FirstOrDefaultAsync(item => item.SessionId == sessionId);
        if (session == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
        if (session.Status != "Scheduled")
        {
            return Result.Fail(ScheduleError.ValidationError, "Chỉ có thể hủy buổi học đang ở trạng thái Scheduled.");
        }

        session.Status = "Cancelled";
        try
        {
            await _db.SaveChangesAsync();
            return Result.Ok("Đã hủy buổi học.");
        }
        catch (Exception exception)
        {
            return FailureFromException(exception, "Không thể hủy buổi học lúc này.");
        }
    }

    public async Task<Result> AssignSubstituteAsync(int sessionId, int lecturerId)
    {
        var session = await _db.ClassSessions.FirstOrDefaultAsync(item => item.SessionId == sessionId);
        if (session == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
        if (session.Status != "Scheduled")
        {
            return Result.Fail(ScheduleError.ValidationError, "Chỉ có thể phân công dạy thay cho buổi Scheduled.");
        }
        if (!await _db.Users.AnyAsync(user => user.UserId == lecturerId
                && user.IsActive && user.Role.RoleName == "Lecturer"))
        {
            return Result.Fail(ScheduleError.InvalidRole, "Giảng viên dạy thay không hợp lệ hoặc đang bị khóa.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var conflicts = await _conflictChecker.PreviewDatesAsync(
                session.RoomId, session.ShiftId, lecturerId, new[] { session.SessionDate },
                ignoreSessionIds: new[] { session.SessionId });
            if (!conflicts.Success || conflicts.Value == null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(conflicts.Error ?? ScheduleError.ValidationError, conflicts.Message);
            }
            if (conflicts.Value.Count > 0)
            {
                await transaction.RollbackAsync();
                return Result.Fail(conflicts.Value[0].Error, FormatConflicts(conflicts.Value));
            }

            session.LecturerId = lecturerId;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok("Đã phân công giảng viên dạy thay.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể phân công giảng viên lúc này.");
        }
    }

    private static ClassWriteDto ToScheduleRequest(Data.Entities.Class entity) => new()
    {
        ClassName = entity.ClassName,
        SubjectId = entity.SubjectId,
        LecturerId = entity.LecturerId,
        FacilityId = entity.FacilityId,
        RoomId = entity.RoomId,
        ShiftId = entity.ShiftId,
        DaysOfWeek = entity.DaysOfWeek,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate
    };

    private static string FormatConflicts(IEnumerable<ScheduleConflictDto> conflicts)
    {
        var groups = conflicts.GroupBy(item => item.Message)
            .Select(group =>
            {
                var dates = group.Select(item => item.Date).Distinct().OrderBy(date => date).ToList();
                var samples = string.Join(", ", dates.Take(3).Select(date => date.ToString("dd/MM/yyyy")));
                var more = dates.Count > 3 ? $" và {dates.Count - 3} ngày khác" : string.Empty;
                return $"{group.Key} ({dates.Count} ngày, ví dụ: {samples}{more})";
            });
        return string.Join("; ", groups.Take(8));
    }

    private static Result FailureFromException(Exception exception, string fallbackMessage)
    {
        var mapped = SqlExceptionMapper.Map(exception);
        return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? fallbackMessage);
    }
}
