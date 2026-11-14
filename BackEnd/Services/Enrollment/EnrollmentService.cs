using System.Data;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos;
using BackEnd.Services.Common;

namespace BackEnd.Services.Enrollment;

public sealed class EnrollmentService : IEnrollmentService
{
    private readonly TutorFlowDbContext _db;

    public EnrollmentService(TutorFlowDbContext db) => _db = db;

    public async Task<Result<EnrollmentOptionsDto>> GetOptionsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var options = new EnrollmentOptionsDto
        {
            Students = await _db.Users.AsNoTracking()
                .Where(user => user.Role.RoleName == "Student" && user.IsActive)
                .OrderBy(user => user.FullName)
                .Select(user => new EnrollmentStudentOptionDto
                {
                    StudentId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email
                }).ToListAsync(),
            Classes = await _db.Classes.AsNoTracking()
                .Where(item => item.Status == "Active" && item.ClassSessions
                    .Any(session => session.Status == "Scheduled" && session.SessionDate >= today))
                .OrderBy(item => item.ClassName)
                .Select(item => new EnrollmentClassOptionDto
                {
                    ClassId = item.ClassId,
                    ClassName = item.ClassName,
                    SubjectName = item.Subject.SubjectName,
                    LecturerName = item.Lecturer.FullName
                }).ToListAsync()
        };
        return Result<EnrollmentOptionsDto>.Ok(options);
    }

    public async Task<Result<List<EnrollmentDto>>> ListAsync(string? search, int? classId, string? status)
    {
        var query = _db.Enrollments.AsNoTracking().AsQueryable();
        if (classId.HasValue) query = query.Where(item => item.ClassId == classId.Value);
        if (status is "Active" or "Dropped") query = query.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Student.FullName.Contains(term)
                || item.Student.Email.Contains(term) || item.Class.ClassName.Contains(term));
        }

        var list = await query.OrderBy(item => item.Class.ClassName).ThenBy(item => item.Student.FullName)
            .Select(item => new EnrollmentDto
            {
                EnrollmentId = item.EnrollmentId,
                StudentId = item.StudentId,
                StudentName = item.Student.FullName,
                StudentEmail = item.Student.Email,
                ClassId = item.ClassId,
                ClassName = item.Class.ClassName,
                EnrollDate = item.EnrollDate,
                Status = item.Status
            }).ToListAsync();
        return Result<List<EnrollmentDto>>.Ok(list);
    }

    public async Task<Result<EnrollmentPreviewDto>> PreviewConflictsAsync(EnrollmentRequestDto request)
    {
        if (request.StudentId <= 0 || request.ClassId <= 0 || request.EnrollDate == default)
            return Result<EnrollmentPreviewDto>.Fail(ScheduleError.ValidationError, "Học viên, lớp và ngày ghi danh là bắt buộc.");

        var student = await _db.Users.AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.UserId == request.StudentId);
        if (student == null || student.Role?.RoleName != "Student" || !student.IsActive)
            return Result<EnrollmentPreviewDto>.Fail(ScheduleError.InvalidRole, "Tài khoản được chọn không phải học viên đang hoạt động.");

        var targetClass = await _db.Classes.AsNoTracking()
            .FirstOrDefaultAsync(item => item.ClassId == request.ClassId && item.Status == "Active");
        if (targetClass == null)
            return Result<EnrollmentPreviewDto>.Fail(ScheduleError.NotFound, "Không tìm thấy lớp đang hoạt động.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstEligibleDate = request.EnrollDate > today ? request.EnrollDate : today;
        var hasUpcomingSession = await _db.ClassSessions.AsNoTracking().AnyAsync(session =>
            session.ClassId == request.ClassId && session.Status == "Scheduled" && session.SessionDate >= firstEligibleDate);
        if (!hasUpcomingSession)
            return Result<EnrollmentPreviewDto>.Fail(ScheduleError.ValidationError,
                "Lớp chưa có buổi học sắp tới từ ngày ghi danh. Hãy chọn lớp có lịch tương lai hoặc nhờ Grand Manager xử lý lịch lớp đang xung đột.");

        var existing = await _db.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.StudentId == request.StudentId && item.ClassId == request.ClassId);
        if (existing?.Status == "Active")
            return Result<EnrollmentPreviewDto>.Ok(new EnrollmentPreviewDto { AlreadyEnrolled = true }, "Học viên đã ghi danh Active vào lớp này.");

        var targetSessions = _db.ClassSessions.AsNoTracking()
            .Where(session => session.ClassId == request.ClassId
                && session.Status != "Cancelled"
                && session.SessionDate >= request.EnrollDate);
        var conflicts = await (
            from enrollment in _db.Enrollments.AsNoTracking()
            join otherSession in _db.ClassSessions.AsNoTracking() on enrollment.ClassId equals otherSession.ClassId
            from targetSession in targetSessions
            where enrollment.StudentId == request.StudentId
                && enrollment.Status == "Active"
                && enrollment.ClassId != request.ClassId
                && otherSession.Status != "Cancelled"
                && otherSession.SessionDate >= enrollment.EnrollDate
                && targetSession.SessionDate == otherSession.SessionDate
                && targetSession.ShiftId == otherSession.ShiftId
            select new EnrollmentConflictDto
            {
                Date = targetSession.SessionDate,
                ShiftName = targetSession.Shift.ShiftName,
                ConflictingClassName = enrollment.Class.ClassName
            }).Distinct().OrderBy(item => item.Date).ThenBy(item => item.ShiftName).ToListAsync();

        return Result<EnrollmentPreviewDto>.Ok(new EnrollmentPreviewDto { Conflicts = conflicts },
            conflicts.Count == 0 ? "Không phát hiện xung đột lịch." : "Học viên có buổi học trùng lịch với lớp khác.");
    }

    public async Task<Result<BulkEnrollmentCheckDto>> PreviewManyAsync(BulkEnrollmentRequestDto request)
    {
        var studentIds = request.StudentIds?.Distinct().ToList() ?? new List<int>();
        if (studentIds.Count == 0 || studentIds.Count > 100 || studentIds.Any(id => id <= 0)
            || request.ClassId <= 0 || request.EnrollDate == default)
            return Result<BulkEnrollmentCheckDto>.Fail(ScheduleError.ValidationError,
                "Chọn từ 1 đến 100 học viên, một lớp và ngày ghi danh hợp lệ.");

        var names = await _db.Users.AsNoTracking()
            .Where(user => studentIds.Contains(user.UserId))
            .Select(user => new { user.UserId, user.FullName })
            .ToDictionaryAsync(user => user.UserId, user => user.FullName);

        var checks = new List<BulkEnrollmentStudentCheckDto>(studentIds.Count);
        foreach (var studentId in studentIds)
        {
            var preview = await PreviewConflictsAsync(new EnrollmentRequestDto
            {
                StudentId = studentId,
                ClassId = request.ClassId,
                EnrollDate = request.EnrollDate
            });
            var studentName = names.GetValueOrDefault(studentId) ?? $"Học viên #{studentId}";

            if (!preview.Success || preview.Value == null)
            {
                checks.Add(new BulkEnrollmentStudentCheckDto
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    Status = "Failed",
                    Message = preview.Message
                });
            }
            else if (preview.Value.AlreadyEnrolled)
            {
                checks.Add(new BulkEnrollmentStudentCheckDto
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    Status = "AlreadyEnrolled",
                    Message = "Học viên đã ghi danh đang hoạt động trong lớp này."
                });
            }
            else if (preview.Value.Conflicts.Count > 0)
            {
                checks.Add(new BulkEnrollmentStudentCheckDto
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    Status = "Conflict",
                    Message = "Có buổi học trùng ngày và ca với lớp khác.",
                    Conflicts = preview.Value.Conflicts
                });
            }
            else
            {
                checks.Add(new BulkEnrollmentStudentCheckDto
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    Status = "Ready",
                    Message = "Có thể ghi danh."
                });
            }
        }

        return Result<BulkEnrollmentCheckDto>.Ok(new BulkEnrollmentCheckDto { Students = checks });
    }

    public async Task<Result<EnrollmentDto>> EnrollAsync(EnrollmentRequestDto request)
    {
        var preview = await PreviewConflictsAsync(request);
        if (!preview.Success || preview.Value == null)
            return Result<EnrollmentDto>.Fail(preview.Error ?? ScheduleError.ValidationError, preview.Message);
        if (preview.Value.AlreadyEnrolled)
            return Result<EnrollmentDto>.Fail(ScheduleError.ValidationError, "Học viên đã ghi danh vào lớp này.");
        if (preview.Value.Conflicts.Count > 0)
            return Result<EnrollmentDto>.Fail(ScheduleError.StudentEnrollConflict,
                "Học viên bị trùng lịch. Hãy xem ngày và lớp đang học trước khi ghi danh.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var enrollment = await _db.Enrollments.FirstOrDefaultAsync(item =>
                item.StudentId == request.StudentId && item.ClassId == request.ClassId);
            if (enrollment == null)
            {
                enrollment = new BackEnd.Data.Entities.Enrollment
                {
                    StudentId = request.StudentId,
                    ClassId = request.ClassId,
                    EnrollDate = request.EnrollDate,
                    Status = "Active"
                };
                _db.Enrollments.Add(enrollment);
            }
            else
            {
                enrollment.EnrollDate = request.EnrollDate;
                enrollment.Status = "Active";
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            var result = await _db.Enrollments.AsNoTracking()
                .Where(item => item.EnrollmentId == enrollment.EnrollmentId)
                .Select(item => new EnrollmentDto
                {
                    EnrollmentId = item.EnrollmentId,
                    StudentId = item.StudentId,
                    StudentName = item.Student.FullName,
                    StudentEmail = item.Student.Email,
                    ClassId = item.ClassId,
                    ClassName = item.Class.ClassName,
                    EnrollDate = item.EnrollDate,
                    Status = item.Status
                }).FirstAsync();
            return Result<EnrollmentDto>.Ok(result, "Đã ghi danh học viên vào lớp.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result<EnrollmentDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể ghi danh học viên lúc này.");
        }
    }

    public async Task<Result<BulkEnrollmentResultDto>> EnrollManyAsync(BulkEnrollmentRequestDto request)
    {
        var check = await PreviewManyAsync(request);
        if (!check.Success || check.Value == null)
            return Result<BulkEnrollmentResultDto>.Fail(check.Error ?? ScheduleError.ValidationError, check.Message);

        var results = new List<BulkEnrollmentStudentResultDto>(check.Value.Students.Count);
        foreach (var student in check.Value.Students)
        {
            if (student.Status != "Ready")
            {
                results.Add(new BulkEnrollmentStudentResultDto
                {
                    StudentId = student.StudentId,
                    StudentName = student.StudentName,
                    Status = student.Status,
                    Message = student.Message,
                    Conflicts = student.Conflicts
                });
                continue;
            }

            var enrolled = await EnrollAsync(new EnrollmentRequestDto
            {
                StudentId = student.StudentId,
                ClassId = request.ClassId,
                EnrollDate = request.EnrollDate
            });
            results.Add(new BulkEnrollmentStudentResultDto
            {
                StudentId = student.StudentId,
                StudentName = student.StudentName,
                Status = enrolled.Success ? "Enrolled"
                    : enrolled.Error == ScheduleError.StudentEnrollConflict ? "Conflict" : "Failed",
                Message = enrolled.Message,
                Conflicts = student.Conflicts
            });
        }

        return Result<BulkEnrollmentResultDto>.Ok(new BulkEnrollmentResultDto { Students = results },
            $"Đã xử lý {results.Count} học viên.");
    }

    public async Task<Result> DropAsync(int enrollmentId)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(item => item.EnrollmentId == enrollmentId);
        if (enrollment == null) return Result.Fail(ScheduleError.NotFound, "Không tìm thấy ghi danh.");
        if (enrollment.Status == "Dropped") return Result.Ok("Học viên đã nghỉ lớp trước đó.");

        enrollment.Status = "Dropped";
        try
        {
            await _db.SaveChangesAsync();
            return Result.Ok("Đã cập nhật trạng thái nghỉ học.");
        }
        catch (Exception exception)
        {
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể cập nhật trạng thái ghi danh.");
        }
    }
}
