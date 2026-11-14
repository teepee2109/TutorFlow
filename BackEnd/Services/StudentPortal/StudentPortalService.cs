using Microsoft.EntityFrameworkCore;
using System.Data;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos;
using BackEnd.Services.Common;
using BackEnd.Services.Tuition;

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
                BillableSessions = item.BillableSessions,
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
                }).ToList(),
                PaymentRequests = item.PaymentRequests.OrderByDescending(paymentRequest => paymentRequest.RequestedAt)
                    .Select(paymentRequest => new PaymentRequestDto
                    {
                        PaymentRequestId = paymentRequest.PaymentRequestId,
                        TuitionId = paymentRequest.TuitionId,
                        StudentId = paymentRequest.StudentId,
                        StudentName = paymentRequest.Student.FullName,
                        StudentEmail = paymentRequest.Student.Email,
                        ClassName = paymentRequest.Tuition.Class.ClassName,
                        PeriodFrom = paymentRequest.Tuition.PeriodFrom,
                        PeriodTo = paymentRequest.Tuition.PeriodTo,
                        AmountRequested = paymentRequest.AmountRequested,
                        PaymentMethod = paymentRequest.PaymentMethod,
                        TransferReference = paymentRequest.TransferReference,
                        Note = paymentRequest.Note,
                        Status = paymentRequest.Status,
                        RequestedAt = paymentRequest.RequestedAt,
                        ReviewedAt = paymentRequest.ReviewedAt,
                        ReviewNote = paymentRequest.ReviewNote,
                        ReviewedByName = paymentRequest.ReviewedByNavigation == null ? null : paymentRequest.ReviewedByNavigation.FullName
                    }).ToList()
            }).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var record in result)
            record.PaymentStatus = TuitionPaymentStatus.GetDisplayStatus(record.TotalAmount, record.AmountPaid, record.DueDate, today);
        return Result<List<TuitionRecordDto>>.Ok(result);
    }

    public async Task<Result<PaymentRequestDto>> CreatePaymentRequestAsync(int tuitionId, PaymentRequestCreateDto request, int studentId)
    {
        if (!IsCurrentStudent(studentId))
            return Result<PaymentRequestDto>.Fail(ScheduleError.InvalidRole, "Only the signed-in student can submit a payment confirmation.");
        if (request.AmountRequested <= 0 || request.AmountRequested > 999999999999m)
            return Result<PaymentRequestDto>.Fail(ScheduleError.ValidationError, "Enter a valid payment amount.");
        if (request.PaymentMethod is not ("Chuyển khoản" or "Tiền mặt"))
            return Result<PaymentRequestDto>.Fail(ScheduleError.ValidationError, "Choose a supported payment method.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var tuition = await _db.Tuitions.Include(item => item.Payments)
                .Include(item => item.Class).Include(item => item.Student)
                .FirstOrDefaultAsync(item => item.TuitionId == tuitionId && item.StudentId == studentId);
            if (tuition == null)
            {
                await transaction.RollbackAsync();
                return Result<PaymentRequestDto>.Fail(ScheduleError.NotFound, "Tuition invoice was not found.");
            }

            var remaining = (tuition.TotalAmount ?? 0m) - tuition.Payments.Sum(item => item.AmountPaid);
            var pending = await _db.PaymentRequests.Where(item => item.TuitionId == tuitionId && item.Status == "Pending")
                .SumAsync(item => (decimal?)item.AmountRequested) ?? 0m;
            var available = remaining - pending;
            if (available <= 0 || request.AmountRequested > available)
            {
                await transaction.RollbackAsync();
                return Result<PaymentRequestDto>.Fail(ScheduleError.ValidationError,
                    available <= 0 ? "This invoice already has a pending payment confirmation." : $"Amount exceeds the unreserved balance ({available:N0} VND).");
            }

            var paymentRequest = new PaymentRequest
            {
                TuitionId = tuitionId,
                StudentId = studentId,
                AmountRequested = request.AmountRequested,
                PaymentMethod = request.PaymentMethod,
                TransferReference = string.IsNullOrWhiteSpace(request.TransferReference) ? null : request.TransferReference.Trim(),
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                Status = "Pending",
                RequestedAt = DateTime.Now
            };
            _db.PaymentRequests.Add(paymentRequest);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<PaymentRequestDto>.Ok(new PaymentRequestDto
            {
                PaymentRequestId = paymentRequest.PaymentRequestId,
                TuitionId = tuition.TuitionId,
                StudentId = studentId,
                StudentName = tuition.Student.FullName,
                StudentEmail = tuition.Student.Email,
                ClassName = tuition.Class.ClassName,
                PeriodFrom = tuition.PeriodFrom,
                PeriodTo = tuition.PeriodTo,
                AmountRequested = paymentRequest.AmountRequested,
                PaymentMethod = paymentRequest.PaymentMethod,
                TransferReference = paymentRequest.TransferReference,
                Note = paymentRequest.Note,
                Status = paymentRequest.Status,
                RequestedAt = paymentRequest.RequestedAt
            }, "Payment confirmation sent for review.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result<PaymentRequestDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Could not submit payment confirmation.");
        }
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
