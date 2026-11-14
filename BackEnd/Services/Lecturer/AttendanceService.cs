using System.Data;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;

namespace BackEnd.Services.Lecturer;

public sealed class AttendanceService : IAttendanceService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "Present", "ExcusedAbsent", "UnexcusedAbsent"
    };

    private readonly TutorFlowDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AttendanceService(TutorFlowDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<AttendanceSessionListDto>>> GetSessionsAsync(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            return Result<List<AttendanceSessionListDto>>.Fail(
                ScheduleError.ValidationError, "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = _db.ClassSessions.AsNoTracking()
            .Where(session => session.SessionDate >= from && session.SessionDate <= to
                && session.SessionDate <= today && session.Status != "Cancelled");

        if (_currentUser.IsInRole("FacilityManager"))
        {
            if (!_currentUser.FacilityId.HasValue)
            {
                return Result<List<AttendanceSessionListDto>>.Fail(
                    ScheduleError.AttendanceForbidden, "Tài khoản Facility Manager chưa được gán cơ sở.");
            }
            var facilityId = _currentUser.FacilityId.Value;
            query = query.Where(session => session.Class.FacilityId == facilityId);
        }
        else if (_currentUser.IsInRole("GrandManager"))
        {
            // Grand Manager may read attendance information across all facilities.
        }
        else if (_currentUser.IsInRole("Lecturer") && _currentUser.UserId.HasValue)
        {
            var lecturerId = _currentUser.UserId.Value;
            query = query.Where(session => session.LecturerId == lecturerId);
        }
        else
        {
            return Result<List<AttendanceSessionListDto>>.Fail(
                ScheduleError.AttendanceForbidden, "Tài khoản không có quyền xem danh sách điểm danh.");
        }

        var sessions = await query
            .OrderByDescending(session => session.SessionDate)
            .ThenBy(session => session.ShiftId)
            .Select(session => new AttendanceSessionListDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                ClassName = session.Class.ClassName,
                SessionDate = session.SessionDate,
                ShiftName = session.Shift.ShiftName,
                RoomNumber = session.Room.RoomNumber,
                FacilityName = session.Class.Facility.FacilityName,
                LecturerName = session.Lecturer.FullName,
                Status = session.Status,
                EligibleStudentCount = _db.Enrollments.Count(enrollment => enrollment.ClassId == session.ClassId
                    && enrollment.Status == "Active" && enrollment.EnrollDate <= session.SessionDate),
                RecordedAttendanceCount = _db.Attendances.Count(attendance => attendance.SessionId == session.SessionId
                    && _db.Enrollments.Any(enrollment => enrollment.ClassId == session.ClassId
                        && enrollment.StudentId == attendance.StudentId
                        && enrollment.Status == "Active" && enrollment.EnrollDate <= session.SessionDate))
            })
            .ToListAsync();

        return Result<List<AttendanceSessionListDto>>.Ok(sessions);
    }

    public async Task<Result<AttendanceRosterDto>> GetRosterAsync(int sessionId)
    {
        var session = await _db.ClassSessions.AsNoTracking()
            .Include(item => item.Class)
            .Include(item => item.Room)
            .Include(item => item.Shift)
            .FirstOrDefaultAsync(item => item.SessionId == sessionId);
        if (session == null)
        {
            return Result<AttendanceRosterDto>.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
        }

        var accessError = ValidateRosterReadAccess(session);
        if (accessError != null)
        {
            return Result<AttendanceRosterDto>.Fail(accessError.Value.Error, accessError.Value.Message);
        }
        var sessionError = ValidateAttendanceSession(session);
        if (sessionError != null)
        {
            return Result<AttendanceRosterDto>.Fail(sessionError.Value.Error, sessionError.Value.Message);
        }

        var enrollments = await _db.Enrollments.AsNoTracking()
            .Where(item => item.ClassId == session.ClassId
                && item.Status == "Active" && item.EnrollDate <= session.SessionDate)
            .Include(item => item.Student)
            .OrderBy(item => item.Student.FullName)
            .ToListAsync();
        var attendanceByStudent = await _db.Attendances.AsNoTracking()
            .Where(item => item.SessionId == sessionId)
            .ToDictionaryAsync(item => item.StudentId);

        return Result<AttendanceRosterDto>.Ok(new AttendanceRosterDto
        {
            SessionId = session.SessionId,
            ClassId = session.ClassId,
            ClassName = session.Class.ClassName,
            SessionDate = session.SessionDate,
            ShiftName = session.Shift.ShiftName,
            RoomNumber = session.Room.RoomNumber,
            Status = session.Status,
            Students = enrollments.Select(enrollment =>
            {
                attendanceByStudent.TryGetValue(enrollment.StudentId, out var attendance);
                return new AttendanceRosterStudentDto
                {
                    StudentId = enrollment.StudentId,
                    FullName = enrollment.Student.FullName,
                    Email = enrollment.Student.Email,
                    AttendanceStatus = attendance?.Status,
                    Note = attendance?.Note
                };
            }).ToList()
        });
    }

    public async Task<Result> SaveAsync(int sessionId, IReadOnlyCollection<AttendanceSaveItemDto> items, int lecturerId)
    {
        if (!IsCurrentLecturer(lecturerId))
        {
            return Result.Fail(ScheduleError.AttendanceForbidden, "Chỉ giảng viên đang đăng nhập được lưu điểm danh.");
        }
        if (items == null)
        {
            return Result.Fail(ScheduleError.ValidationError, "Danh sách điểm danh không hợp lệ.");
        }
        if (items.Any(item => !ValidStatuses.Contains(item.Status)))
        {
            return Result.Fail(ScheduleError.ValidationError, "Trạng thái điểm danh không hợp lệ.");
        }
        if (items.Select(item => item.StudentId).Distinct().Count() != items.Count)
        {
            return Result.Fail(ScheduleError.ValidationError, "Danh sách điểm danh có học viên bị lặp.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var session = await _db.ClassSessions
                .FirstOrDefaultAsync(item => item.SessionId == sessionId);
            if (session == null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
            }

            var accessError = ValidateLecturerAccess(session);
            if (accessError != null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(accessError.Value.Error, accessError.Value.Message);
            }
            if (session.Status == "Completed")
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.AttendanceSessionInvalid, "Buổi học đã hoàn tất; không thể sửa điểm danh.");
            }

            var sessionError = ValidateAttendanceSession(session);
            if (sessionError != null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(sessionError.Value.Error, sessionError.Value.Message);
            }

            var studentIds = items.Select(item => item.StudentId).ToArray();
            var enrolledStudentIds = await _db.Enrollments
                .Where(enrollment => enrollment.ClassId == session.ClassId
                    && enrollment.Status == "Active"
                    && enrollment.EnrollDate <= session.SessionDate
                    && studentIds.Contains(enrollment.StudentId))
                .Select(enrollment => enrollment.StudentId)
                .ToListAsync();
            var enrolledSet = enrolledStudentIds.ToHashSet();
            if (studentIds.Any(studentId => !enrolledSet.Contains(studentId)))
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.AttendanceNotEnrolled,
                    "Có học viên chưa ghi danh Active vào lớp trước ngày buổi học.");
            }

            var existing = await _db.Attendances
                .Where(item => item.SessionId == sessionId && studentIds.Contains(item.StudentId))
                .ToDictionaryAsync(item => item.StudentId);
            var now = DateTime.Now;
            foreach (var item in items)
            {
                var note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim();
                if (existing.TryGetValue(item.StudentId, out var attendance))
                {
                    attendance.Status = item.Status;
                    attendance.Note = note;
                    attendance.UpdatedBy = lecturerId;
                    attendance.UpdatedAt = now;
                }
                else
                {
                    _db.Attendances.Add(new Attendance
                    {
                        SessionId = sessionId,
                        StudentId = item.StudentId,
                        Status = item.Status,
                        Note = note,
                        RecordedBy = lecturerId,
                        RecordedAt = now
                    });
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok($"Đã lưu điểm danh cho {items.Count} học viên.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể lưu điểm danh lúc này.");
        }
    }

    public async Task<Result> CompleteSessionAsync(int sessionId, int lecturerId)
    {
        if (!IsCurrentLecturer(lecturerId))
        {
            return Result.Fail(ScheduleError.AttendanceForbidden, "Chỉ giảng viên đang đăng nhập được hoàn tất buổi học.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var session = await _db.ClassSessions.FirstOrDefaultAsync(item => item.SessionId == sessionId);
            if (session == null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học.");
            }

            var accessError = ValidateLecturerAccess(session);
            if (accessError != null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(accessError.Value.Error, accessError.Value.Message);
            }
            var sessionError = ValidateAttendanceSession(session);
            if (sessionError != null)
            {
                await transaction.RollbackAsync();
                return Result.Fail(sessionError.Value.Error, sessionError.Value.Message);
            }
            if (session.Status == "Completed")
            {
                await transaction.CommitAsync();
                return Result.Ok("Buổi học đã được hoàn tất trước đó.");
            }

            var eligibleStudentIds = await _db.Enrollments
                .Where(item => item.ClassId == session.ClassId
                    && item.Status == "Active"
                    && item.EnrollDate <= session.SessionDate)
                .Select(item => item.StudentId)
                .ToListAsync();
            var markedCount = await _db.Attendances.CountAsync(item =>
                item.SessionId == sessionId && eligibleStudentIds.Contains(item.StudentId));
            if (markedCount < eligibleStudentIds.Count)
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.ValidationError,
                    $"Chưa điểm danh đủ học viên ({markedCount}/{eligibleStudentIds.Count}). Hãy lưu điểm danh cho toàn bộ danh sách trước khi hoàn tất.");
            }

            session.Status = "Completed";
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok("Đã hoàn tất buổi học.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể hoàn tất buổi học lúc này.");
        }
    }

    private bool IsCurrentLecturer(int lecturerId) =>
        _currentUser.IsInRole("Lecturer") && _currentUser.UserId == lecturerId;

    private (ScheduleError Error, string Message)? ValidateRosterReadAccess(ClassSession session)
    {
        if (_currentUser.IsInRole("Lecturer"))
        {
            return ValidateLecturerAccess(session);
        }

        if (_currentUser.IsInRole("GrandManager"))
        {
            return null;
        }

        if (_currentUser.IsInRole("FacilityManager")
            && _currentUser.FacilityId.HasValue
            && session.Class.FacilityId == _currentUser.FacilityId.Value)
        {
            return null;
        }

        return (ScheduleError.AttendanceForbidden,
            "Chỉ giảng viên phụ trách, Facility Manager của cơ sở hoặc Grand Manager được xem danh sách này.");
    }

    private (ScheduleError Error, string Message)? ValidateLecturerAccess(ClassSession session)
    {
        if (!_currentUser.IsInRole("Lecturer") || !_currentUser.UserId.HasValue
            || session.LecturerId != _currentUser.UserId.Value)
        {
            return (ScheduleError.AttendanceForbidden, "Chỉ giảng viên được phân công cho buổi học này mới được điểm danh.");
        }
        return null;
    }

    private static (ScheduleError Error, string Message)? ValidateAttendanceSession(ClassSession session)
    {
        if (session.Status == "Cancelled" || session.SessionDate > DateOnly.FromDateTime(DateTime.Today))
        {
            return (ScheduleError.AttendanceSessionInvalid, "Không thể điểm danh buổi đã hủy hoặc chưa diễn ra.");
        }
        return null;
    }

    private static Result FailureFromException(Exception exception, string fallbackMessage)
    {
        var mapped = SqlExceptionMapper.Map(exception);
        return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? fallbackMessage);
    }
}
