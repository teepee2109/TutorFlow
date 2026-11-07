using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Dtos;
using BackEnd.Services.Common;

namespace BackEnd.Services.StudentPortal;

public sealed class StudentPortalService : IStudentPortalService
{
    private readonly TutorFlowDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StudentPortalService(TutorFlowDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<Result<List<StudentPortalScheduleItemDto>>> GetMyScheduleAsync(int studentId, DateOnly startDate, DateOnly endDate) =>
        GetScheduleAsync(studentId, startDate, endDate);

    public async Task<Result<List<StudentPortalScheduleItemDto>>> GetMyAttendanceAsync(int studentId, DateOnly startDate, DateOnly endDate)
    {
        var result = await GetScheduleAsync(studentId, startDate, endDate);
        if (!result.Success || result.Value == null) return result;
        return Result<List<StudentPortalScheduleItemDto>>.Ok(result.Value
            .Where(item => item.AttendanceStatus != null || item.SessionStatus == "Completed").ToList());
    }

    public async Task<Result<List<TuitionRecordDto>>> GetMyTuitionAsync(int studentId)
    {
        if (!IsCurrentStudent(studentId))
            return Result<List<TuitionRecordDto>>.Fail(ScheduleError.InvalidRole, "Chỉ học viên đang đăng nhập được xem học phí của mình.");

        var result = await _db.Tuitions.AsNoTracking().Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.PeriodFrom)
            .Select(item => new TuitionRecordDto
            {
                TuitionId = item.TuitionId,
                StudentId = item.StudentId,
                StudentName = item.Student.FullName,
                StudentEmail = item.Student.Email,
                ClassId = item.ClassId,
                ClassName = item.Class.ClassName,
                PeriodFrom = item.PeriodFrom,
                PeriodTo = item.PeriodTo,
                SessionsAttended = item.SessionsAttended,
                AmountPerSession = item.AmountPerSession,
                TotalAmount = item.TotalAmount ?? 0,
                AmountPaid = item.Payments.Sum(payment => (decimal?)payment.AmountPaid) ?? 0,
                RemainingAmount = (item.TotalAmount ?? 0) - (item.Payments.Sum(payment => (decimal?)payment.AmountPaid) ?? 0),
                PaymentStatus = item.PaymentStatus,
                DueDate = item.DueDate,
                Payments = item.Payments.OrderByDescending(payment => payment.PaymentDate).Select(payment => new TuitionPaymentDto
                {
                    PaymentId = payment.PaymentId,
                    AmountPaid = payment.AmountPaid,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    ReceiptNumber = payment.ReceiptNumber,
                    ReceivedByName = payment.ReceivedByNavigation == null ? null : payment.ReceivedByNavigation.FullName
                }).ToList()
            }).ToListAsync();
        return Result<List<TuitionRecordDto>>.Ok(result);
    }

    private async Task<Result<List<StudentPortalScheduleItemDto>>> GetScheduleAsync(int studentId, DateOnly startDate, DateOnly endDate)
    {
        if (!IsCurrentStudent(studentId))
            return Result<List<StudentPortalScheduleItemDto>>.Fail(ScheduleError.InvalidRole, "Chỉ học viên đang đăng nhập được xem lịch và điểm danh của mình.");
        if (startDate == default || endDate == default || endDate < startDate || endDate.DayNumber - startDate.DayNumber > 366)
            return Result<List<StudentPortalScheduleItemDto>>.Fail(ScheduleError.ValidationError, "Khoảng ngày xem lịch không hợp lệ.");

        var sessions = await (
            from enrollment in _db.Enrollments.AsNoTracking()
            join session in _db.ClassSessions.AsNoTracking() on enrollment.ClassId equals session.ClassId
            where enrollment.StudentId == studentId && enrollment.Status == "Active"
                && enrollment.EnrollDate <= session.SessionDate
                && session.SessionDate >= startDate && session.SessionDate <= endDate
            orderby session.SessionDate, session.Shift.StartTime
            select new StudentPortalScheduleItemDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                ClassName = session.Class.ClassName,
                SessionDate = session.SessionDate,
                ShiftName = session.Shift.ShiftName,
                StartTime = session.Shift.StartTime,
                EndTime = session.Shift.EndTime,
                RoomNumber = session.Room.RoomNumber,
                FacilityName = session.Room.Facility.FacilityName,
                LecturerName = session.Lecturer.FullName,
                SessionStatus = session.Status,
                AttendanceStatus = session.Attendances.Where(item => item.StudentId == studentId)
                    .Select(item => item.Status).FirstOrDefault(),
                AttendanceNote = session.Attendances.Where(item => item.StudentId == studentId)
                    .Select(item => item.Note).FirstOrDefault()
            }).ToListAsync();
        return Result<List<StudentPortalScheduleItemDto>>.Ok(sessions);
    }

    private bool IsCurrentStudent(int studentId) => _currentUser.IsInRole("Student")
        && _currentUser.UserId == studentId;
}
