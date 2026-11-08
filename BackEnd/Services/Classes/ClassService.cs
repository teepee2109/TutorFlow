using System.Data;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Classes;

public sealed class ClassService : IClassService
{
    private readonly TutorFlowDbContext _db;
    private readonly IScheduleConflictChecker _conflictChecker;

    public ClassService(TutorFlowDbContext db, IScheduleConflictChecker conflictChecker)
    {
        _db = db;
        _conflictChecker = conflictChecker;
    }

    public async Task<Result<List<ClassListItemDto>>> GetClassesAsync(
        string? search = null,
        string? status = null,
        int? subjectId = null,
        int? lecturerId = null)
    {
        var query = _db.Classes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item => item.ClassName.Contains(keyword)
                || item.Subject.SubjectName.Contains(keyword)
                || item.Lecturer.FullName.Contains(keyword));
        }
        if (status is "Active" or "Closed") query = query.Where(item => item.Status == status);
        if (subjectId.HasValue) query = query.Where(item => item.SubjectId == subjectId.Value);
        if (lecturerId.HasValue) query = query.Where(item => item.LecturerId == lecturerId.Value);

        var classes = await query
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new ClassListItemDto
            {
                ClassId = item.ClassId,
                ClassName = item.ClassName,
                SubjectId = item.SubjectId,
                SubjectName = item.Subject.SubjectName,
                LecturerId = item.LecturerId,
                LecturerName = item.Lecturer.FullName,
                FacilityId = item.FacilityId,
                FacilityName = item.Facility.FacilityName,
                RoomId = item.RoomId,
                RoomNumber = item.Room.RoomNumber,
                ShiftId = item.ShiftId,
                ShiftName = item.Shift.ShiftName,
                DaysOfWeek = item.DaysOfWeek,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                SessionCount = item.ClassSessions.Count
            })
            .ToListAsync();

        return Result<List<ClassListItemDto>>.Ok(classes);
    }

    public async Task<Result<ClassDetailDto>> GetClassAsync(int classId)
    {
        var entity = await _db.Classes.AsNoTracking()
            .Include(item => item.Subject)
            .Include(item => item.Lecturer)
            .Include(item => item.Facility)
            .Include(item => item.Room)
            .Include(item => item.Shift)
            .Include(item => item.ClassSessions).ThenInclude(session => session.Room)
            .Include(item => item.ClassSessions).ThenInclude(session => session.Shift)
            .Include(item => item.ClassSessions).ThenInclude(session => session.Lecturer)
            .FirstOrDefaultAsync(item => item.ClassId == classId);

        if (entity == null)
        {
            return Result<ClassDetailDto>.Fail(ScheduleError.NotFound, "Không tìm thấy lớp học.");
        }

        return Result<ClassDetailDto>.Ok(ToDetailDto(entity));
    }

    public async Task<Result<ClassOptionsDto>> GetOptionsAsync()
    {
        var options = new ClassOptionsDto
        {
            Subjects = await _db.Subjects.AsNoTracking().OrderBy(item => item.SubjectName)
                .Select(item => new ClassLookupOptionDto { Id = item.SubjectId, Name = item.SubjectName }).ToListAsync(),
            Lecturers = await _db.Users.AsNoTracking()
                .Where(user => user.IsActive && user.Role.RoleName == "Lecturer")
                .OrderBy(user => user.FullName)
                .Select(user => new ClassLookupOptionDto { Id = user.UserId, Name = user.FullName }).ToListAsync(),
            Facilities = await _db.Facilities.AsNoTracking().OrderBy(item => item.FacilityName)
                .Select(item => new ClassLookupOptionDto { Id = item.FacilityId, Name = item.FacilityName }).ToListAsync(),
            Rooms = await _db.Rooms.AsNoTracking().Where(room => room.Condition == "Open")
                .OrderBy(room => room.FacilityId).ThenBy(room => room.RoomNumber)
                .Select(room => new ClassLookupOptionDto
                {
                    Id = room.RoomId,
                    Name = room.RoomNumber,
                    FacilityId = room.FacilityId,
                    Capacity = room.Capacity,
                    Condition = room.Condition
                }).ToListAsync(),
            Shifts = await _db.Shifts.AsNoTracking().OrderBy(item => item.ShiftId)
                .Select(item => new ClassLookupOptionDto { Id = item.ShiftId, Name = item.ShiftName }).ToListAsync()
        };

        return Result<ClassOptionsDto>.Ok(options);
    }

    public async Task<Result<List<ScheduleConflictDto>>> PreviewConflictsAsync(ClassWriteDto request, int? excludeClassId = null)
    {
        var validation = await ValidateRequestAsync(request);
        if (!validation.Success)
        {
            return Result<List<ScheduleConflictDto>>.Fail(validation.Error ?? ScheduleError.ValidationError, validation.Message);
        }

        return await _conflictChecker.PreviewClassConflictsAsync(request, excludeClassId);
    }

    public async Task<Result<ClassDetailDto>> CreateAsync(ClassWriteDto request, int createdBy)
    {
        var validation = await ValidateRequestAsync(request);
        if (!validation.Success)
        {
            return Result<ClassDetailDto>.Fail(validation.Error ?? ScheduleError.ValidationError, validation.Message);
        }

        if (!ClassScheduleDates.TryCreate(request, out var dates, out var dateError))
        {
            return Result<ClassDetailDto>.Fail(ScheduleError.ValidationError, dateError);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var conflicts = await _conflictChecker.PreviewClassConflictsAsync(request);
            if (!conflicts.Success || conflicts.Value == null)
            {
                await transaction.RollbackAsync();
                return Result<ClassDetailDto>.Fail(conflicts.Error ?? ScheduleError.ValidationError, conflicts.Message);
            }

            if (conflicts.Value.Count > 0)
            {
                await transaction.RollbackAsync();
                return Result<ClassDetailDto>.Fail(conflicts.Value[0].Error, FormatConflicts(conflicts.Value));
            }

            var entity = new Class
            {
                ClassName = request.ClassName.Trim(),
                SubjectId = request.SubjectId,
                LecturerId = request.LecturerId,
                FacilityId = request.FacilityId,
                RoomId = request.RoomId,
                ShiftId = request.ShiftId,
                DaysOfWeek = string.Join(',', request.DaysOfWeek.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).Distinct().OrderBy(day => day)),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = "Active",
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _db.Classes.Add(entity);
            await _db.SaveChangesAsync();
            _db.ClassSessions.AddRange(dates.Select(date => new ClassSession
            {
                ClassId = entity.ClassId,
                SessionDate = date,
                RoomId = entity.RoomId,
                ShiftId = entity.ShiftId,
                LecturerId = entity.LecturerId,
                Status = "Scheduled"
            }));
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetClassAsync(entity.ClassId);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException<ClassDetailDto>(exception, "Không thể tạo lớp học lúc này.");
        }
    }

    public async Task<Result<ClassDetailDto>> UpdateAsync(int classId, ClassWriteDto request)
    {
        var validation = await ValidateRequestAsync(request);
        if (!validation.Success)
        {
            return Result<ClassDetailDto>.Fail(validation.Error ?? ScheduleError.ValidationError, validation.Message);
        }

        if (!ClassScheduleDates.TryCreate(request, out var generatedDates, out var dateError))
        {
            return Result<ClassDetailDto>.Fail(ScheduleError.ValidationError, dateError);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var targetDates = generatedDates.Where(date => date >= today).ToHashSet();
        var entity = await _db.Classes.FirstOrDefaultAsync(item => item.ClassId == classId);
        if (entity == null)
        {
            return Result<ClassDetailDto>.Fail(ScheduleError.NotFound, "Không tìm thấy lớp học.");
        }
        if (entity.Status != "Active")
        {
            return Result<ClassDetailDto>.Fail(ScheduleError.ValidationError, "Không thể chỉnh sửa lớp đã đóng.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (targetDates.Count > 0)
            {
                var conflicts = await _conflictChecker.PreviewDatesAsync(
                    request.RoomId, request.ShiftId, request.LecturerId, targetDates, classId);
                if (!conflicts.Success || conflicts.Value == null)
                {
                    await transaction.RollbackAsync();
                    return Result<ClassDetailDto>.Fail(conflicts.Error ?? ScheduleError.ValidationError, conflicts.Message);
                }
                if (conflicts.Value.Count > 0)
                {
                    await transaction.RollbackAsync();
                    return Result<ClassDetailDto>.Fail(conflicts.Value[0].Error, FormatConflicts(conflicts.Value));
                }
            }

            entity.ClassName = request.ClassName.Trim();
            entity.SubjectId = request.SubjectId;
            entity.LecturerId = request.LecturerId;
            entity.FacilityId = request.FacilityId;
            entity.RoomId = request.RoomId;
            entity.ShiftId = request.ShiftId;
            entity.DaysOfWeek = string.Join(',', request.DaysOfWeek.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).Distinct().OrderBy(day => day));
            entity.StartDate = request.StartDate;
            entity.EndDate = request.EndDate;

            var futureSessions = await _db.ClassSessions
                .Where(session => session.ClassId == classId && session.Status == "Scheduled" && session.SessionDate >= today)
                .ToListAsync();
            foreach (var session in futureSessions)
            {
                if (targetDates.Remove(session.SessionDate))
                {
                    session.RoomId = request.RoomId;
                    session.ShiftId = request.ShiftId;
                    session.LecturerId = request.LecturerId;
                }
                else
                {
                    session.Status = "Cancelled";
                }
            }

            var occupiedClassDates = await _db.ClassSessions.AsNoTracking()
                .Where(session => session.ClassId == classId)
                .Select(session => session.SessionDate)
                .ToListAsync();
            var occupiedDateSet = occupiedClassDates.ToHashSet();
            var newSessions = targetDates
                .Where(date => !occupiedDateSet.Contains(date))
                .Select(date => new ClassSession
                {
                    ClassId = classId,
                    SessionDate = date,
                    RoomId = request.RoomId,
                    ShiftId = request.ShiftId,
                    LecturerId = request.LecturerId,
                    Status = "Scheduled"
                });
            _db.ClassSessions.AddRange(newSessions);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return await GetClassAsync(classId);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException<ClassDetailDto>(exception, "Không thể cập nhật lớp học lúc này.");
        }
    }

    public async Task<Result> ChangeLecturerAsync(int classId, int lecturerId)
    {
        var entity = await _db.Classes.FirstOrDefaultAsync(item => item.ClassId == classId);
        if (entity == null)
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy lớp học.");
        }
        if (entity.Status != "Active")
        {
            return Result.Fail(ScheduleError.ValidationError, "Không thể đổi giảng viên của lớp đã đóng.");
        }
        if (!await IsActiveLecturerAsync(lecturerId))
        {
            return Result.Fail(ScheduleError.InvalidRole, "Giảng viên mới không hợp lệ hoặc đang bị khóa.");
        }
        if (entity.LecturerId == lecturerId)
        {
            return Result.Ok("Giảng viên của lớp không thay đổi.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var futureSessions = await _db.ClassSessions
            .Where(session => session.ClassId == classId && session.Status == "Scheduled" && session.SessionDate >= today)
            .ToListAsync();

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var conflicts = new List<ScheduleConflictDto>();
            var ownSessionIds = futureSessions.Select(session => session.SessionId).ToArray();
            foreach (var group in futureSessions.GroupBy(session => new { session.RoomId, session.ShiftId }))
            {
                var result = await _conflictChecker.PreviewDatesAsync(
                    group.Key.RoomId, group.Key.ShiftId, lecturerId,
                    group.Select(session => session.SessionDate).ToArray(),
                    ignoreSessionIds: ownSessionIds);
                if (result.Success && result.Value != null) conflicts.AddRange(result.Value);
                else if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return Result.Fail(result.Error ?? ScheduleError.ValidationError, result.Message);
                }
            }

            if (conflicts.Count > 0)
            {
                await transaction.RollbackAsync();
                return Result.Fail(conflicts[0].Error, FormatConflicts(conflicts));
            }

            entity.LecturerId = lecturerId;
            foreach (var session in futureSessions) session.LecturerId = lecturerId;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok("Đã đổi giảng viên cho lớp và các buổi học sắp tới.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể đổi giảng viên lúc này.");
        }
    }

    public async Task<Result> CloseAsync(int classId)
    {
        var entity = await _db.Classes.FirstOrDefaultAsync(item => item.ClassId == classId);
        if (entity == null)
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy lớp học.");
        }
        if (entity.Status == "Closed")
        {
            return Result.Ok("Lớp học đã được đóng trước đó.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            entity.Status = "Closed";
            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcomingSessions = await _db.ClassSessions
                .Where(session => session.ClassId == classId && session.Status == "Scheduled" && session.SessionDate >= today)
                .ToListAsync();
            foreach (var session in upcomingSessions) session.Status = "Cancelled";

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok($"Đã đóng lớp và hủy {upcomingSessions.Count} buổi học sắp tới.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể đóng lớp lúc này.");
        }
    }

    private async Task<Result> ValidateRequestAsync(ClassWriteDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ClassName) || request.ClassName.Trim().Length > 100)
        {
            return Result.Fail(ScheduleError.ValidationError, "Tên lớp bắt buộc và không được vượt quá 100 ký tự.");
        }
        if (!ClassScheduleDates.TryCreate(request, out _, out var dateError))
        {
            return Result.Fail(ScheduleError.ValidationError, dateError);
        }
        if (!await _db.Subjects.AnyAsync(subject => subject.SubjectId == request.SubjectId))
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy môn học.");
        }
        if (!await _db.Facilities.AnyAsync(facility => facility.FacilityId == request.FacilityId))
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy cơ sở.");
        }
        if (!await _db.Shifts.AnyAsync(shift => shift.ShiftId == request.ShiftId))
        {
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy ca học.");
        }
        if (!await _db.Rooms.AnyAsync(room => room.RoomId == request.RoomId && room.FacilityId == request.FacilityId))
        {
            return Result.Fail(ScheduleError.ValidationError, "Phòng đã chọn không thuộc cơ sở của lớp.");
        }
        if (!await IsActiveLecturerAsync(request.LecturerId))
        {
            return Result.Fail(ScheduleError.InvalidRole, "Giảng viên phải là tài khoản Lecturer đang hoạt động.");
        }
        return Result.Ok();
    }

    private Task<bool> IsActiveLecturerAsync(int userId) => _db.Users
        .AnyAsync(user => user.UserId == userId && user.IsActive && user.Role.RoleName == "Lecturer");

    private async Task<ClassDetailDto> BuildDetailAsync(int classId)
    {
        var entity = await _db.Classes.AsNoTracking()
            .Include(item => item.Subject)
            .Include(item => item.Lecturer)
            .Include(item => item.Facility)
            .Include(item => item.Room)
            .Include(item => item.Shift)
            .Include(item => item.ClassSessions).ThenInclude(session => session.Room)
            .Include(item => item.ClassSessions).ThenInclude(session => session.Shift)
            .Include(item => item.ClassSessions).ThenInclude(session => session.Lecturer)
            .FirstAsync(item => item.ClassId == classId);
        return ToDetailDto(entity);
    }

    private static ClassDetailDto ToDetailDto(Class entity) => new()
    {
        ClassId = entity.ClassId,
        ClassName = entity.ClassName,
        SubjectId = entity.SubjectId,
        SubjectName = entity.Subject.SubjectName,
        LecturerId = entity.LecturerId,
        LecturerName = entity.Lecturer.FullName,
        FacilityId = entity.FacilityId,
        FacilityName = entity.Facility.FacilityName,
        RoomId = entity.RoomId,
        RoomNumber = entity.Room.RoomNumber,
        ShiftId = entity.ShiftId,
        ShiftName = entity.Shift.ShiftName,
        DaysOfWeek = entity.DaysOfWeek,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        Status = entity.Status,
        SessionCount = entity.ClassSessions.Count,
        Sessions = entity.ClassSessions.OrderBy(session => session.SessionDate).Select(session => new ClassSessionDto
        {
            SessionId = session.SessionId,
            SessionDate = session.SessionDate,
            RoomId = session.RoomId,
            RoomNumber = session.Room.RoomNumber,
            ShiftId = session.ShiftId,
            ShiftName = session.Shift.ShiftName,
            LecturerId = session.LecturerId,
            LecturerName = session.Lecturer.FullName,
            Status = session.Status
        }).ToList()
    };

    private static string FormatConflicts(IEnumerable<ScheduleConflictDto> conflicts) => string.Join("; ",
        conflicts.Select(item => $"{item.Date:dd/MM/yyyy}: {item.Message}"));

    private static Result<T> FailureFromException<T>(Exception exception, string fallbackMessage)
    {
        var mapped = SqlExceptionMapper.Map(exception);
        return Result<T>.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? fallbackMessage);
    }

    private static Result FailureFromException(Exception exception, string fallbackMessage)
    {
        var mapped = SqlExceptionMapper.Map(exception);
        return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? fallbackMessage);
    }
}
