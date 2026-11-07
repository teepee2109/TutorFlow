using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos;
using BackEnd.Services.Common;

namespace BackEnd.Services.Tuition;

public sealed class TuitionService : ITuitionService
{
    private const decimal DefaultRate = 200000m;
    private readonly TutorFlowDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TuitionService(TutorFlowDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<TuitionPreviewItemDto>>> PreviewAsync(DateOnly from, DateOnly to, int? classId)
    {
        var periodError = ValidatePeriod(from, to);
        if (periodError != null) return Result<List<TuitionPreviewItemDto>>.Fail(ScheduleError.ValidationError, periodError);

        var rate = await GetRateAsync();
        var source = await _db.Set<StudentAttendanceTuitionDto>()
            .FromSqlInterpolated($"SELECT * FROM dbo.fn_StudentAttendanceTuition({from}, {to})")
            .AsNoTracking()
            .ToListAsync();
        var role = _currentUser.Role;
        if (role == "Lecturer")
        {
            var lecturerId = _currentUser.UserId;
            var lecturerClassIds = lecturerId.HasValue
                ? await _db.Classes.AsNoTracking().Where(item => item.LecturerId == lecturerId.Value)
                    .Select(item => item.ClassId).ToListAsync()
                : new List<int>();
            var lecturerClassIdSet = lecturerClassIds.ToHashSet();
            source = source.Where(row => lecturerClassIdSet.Contains(row.ClassId)).ToList();
        }
        if (classId.HasValue) source = source.Where(row => row.ClassId == classId.Value).ToList();

        var existing = (await _db.Tuitions.AsNoTracking()
            .Where(item => item.PeriodFrom == from && item.PeriodTo == to)
            .Select(item => new { item.StudentId, item.ClassId }).ToListAsync())
            .Select(item => $"{item.StudentId}:{item.ClassId}").ToHashSet();

        var items = source.Select(row => new TuitionPreviewItemDto
        {
            StudentId = row.StudentId,
            StudentName = row.StudentName,
            ClassId = row.ClassId,
            ClassName = row.ClassName,
            PresentCount = row.PresentCount,
            ExcusedCount = row.ExcusedCount,
            UnexcusedCount = row.UnexcusedCount,
            TotalSessions = row.TotalSessions,
            AmountPerSession = rate,
            TuitionAmount = row.PresentCount * rate,
            AlreadyIssued = existing.Contains($"{row.StudentId}:{row.ClassId}")
        }).OrderBy(item => item.ClassName).ThenBy(item => item.StudentName).ToList();

        return Result<List<TuitionPreviewItemDto>>.Ok(items);
    }

    public async Task<Result<List<TuitionRecordDto>>> ListAsync(DateOnly from, DateOnly to, int? classId)
    {
        var periodError = ValidatePeriod(from, to);
        if (periodError != null) return Result<List<TuitionRecordDto>>.Fail(ScheduleError.ValidationError, periodError);

        var query = _db.Tuitions.AsNoTracking()
            .Where(item => item.PeriodFrom <= to && item.PeriodTo >= from);
        if (classId.HasValue) query = query.Where(item => item.ClassId == classId.Value);
        if (_currentUser.IsInRole("Lecturer") && _currentUser.UserId.HasValue)
            query = query.Where(item => item.Class.LecturerId == _currentUser.UserId.Value);

        var records = await query.OrderByDescending(item => item.PeriodFrom)
            .ThenBy(item => item.Class.ClassName).ThenBy(item => item.Student.FullName)
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
        return Result<List<TuitionRecordDto>>.Ok(records);
    }

    public async Task<Result<TuitionIssueResultDto>> IssueAsync(TuitionIssueRequestDto request)
    {
        var periodError = ValidatePeriod(request.PeriodFrom, request.PeriodTo);
        if (periodError != null) return Result<TuitionIssueResultDto>.Fail(ScheduleError.ValidationError, periodError);
        if (!_currentUser.IsInRole("GrandManager"))
            return Result<TuitionIssueResultDto>.Fail(ScheduleError.InvalidRole, "Chỉ Grand Manager được phát hành học phí.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var preview = await PreviewAsync(request.PeriodFrom, request.PeriodTo, request.ClassId);
            if (!preview.Success || preview.Value == null)
            {
                await transaction.RollbackAsync();
                return Result<TuitionIssueResultDto>.Fail(preview.Error ?? ScheduleError.ValidationError, preview.Message);
            }

            var eligible = preview.Value.Where(item => !item.AlreadyIssued && item.PresentCount > 0).ToList();
            var skippedExisting = preview.Value.Count(item => item.AlreadyIssued);
            var skippedNoAttendance = preview.Value.Count(item => !item.AlreadyIssued && item.PresentCount == 0);
            foreach (var row in eligible)
            {
                _db.Tuitions.Add(new BackEnd.Data.Entities.Tuition
                {
                    StudentId = row.StudentId,
                    ClassId = row.ClassId,
                    PeriodFrom = request.PeriodFrom,
                    PeriodTo = request.PeriodTo,
                    SessionsAttended = row.PresentCount,
                    AmountPerSession = row.AmountPerSession,
                    PaymentStatus = "Unpaid",
                    DueDate = request.PeriodTo.AddDays(7),
                    CreatedAt = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result<TuitionIssueResultDto>.Ok(new TuitionIssueResultDto
            {
                IssuedCount = eligible.Count,
                SkippedExistingCount = skippedExisting,
                SkippedNoAttendanceCount = skippedNoAttendance
            }, $"Đã phát hành {eligible.Count} khoản học phí; bỏ qua {skippedExisting} khoản đã phát hành và {skippedNoAttendance} trường hợp chưa có buổi Present.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result<TuitionIssueResultDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể phát hành học phí lúc này.");
        }
    }

    public async Task<Result<TuitionRecordDto>> RecordPaymentAsync(int tuitionId, PaymentCreateDto request, int receiverId)
    {
        if (!_currentUser.IsInRole("GrandManager") || _currentUser.UserId != receiverId)
            return Result<TuitionRecordDto>.Fail(ScheduleError.InvalidRole, "Chỉ Grand Manager đang đăng nhập được ghi nhận thanh toán.");
        if (request.AmountPaid <= 0 || request.AmountPaid > 999999999999m)
            return Result<TuitionRecordDto>.Fail(ScheduleError.ValidationError, "Số tiền thanh toán phải lớn hơn 0.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var tuition = await _db.Tuitions.Include(item => item.Payments)
                .FirstOrDefaultAsync(item => item.TuitionId == tuitionId);
            if (tuition == null)
            {
                await transaction.RollbackAsync();
                return Result<TuitionRecordDto>.Fail(ScheduleError.NotFound, "Không tìm thấy khoản học phí.");
            }

            var total = tuition.TotalAmount ?? 0m;
            var paid = tuition.Payments.Sum(item => item.AmountPaid);
            var remaining = total - paid;
            if (remaining <= 0)
            {
                await transaction.RollbackAsync();
                return Result<TuitionRecordDto>.Fail(ScheduleError.ValidationError, "Khoản học phí này đã thanh toán đủ.");
            }
            if (request.AmountPaid > remaining)
            {
                await transaction.RollbackAsync();
                return Result<TuitionRecordDto>.Fail(ScheduleError.ValidationError, $"Số tiền vượt số dư còn lại ({remaining:N0} VND).");
            }

            _db.Payments.Add(new Payment
            {
                TuitionId = tuitionId,
                AmountPaid = request.AmountPaid,
                PaymentDate = DateTime.Now,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim(),
                ReceiptNumber = string.IsNullOrWhiteSpace(request.ReceiptNumber) ? null : request.ReceiptNumber.Trim(),
                ReceivedBy = receiverId
            });
            tuition.PaymentStatus = paid + request.AmountPaid >= total ? "Paid" : "Unpaid";
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var record = await ListOneAsync(tuitionId);
            return Result<TuitionRecordDto>.Ok(record!, "Đã ghi nhận thanh toán.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result<TuitionRecordDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể ghi nhận thanh toán lúc này.");
        }
    }

    private async Task<TuitionRecordDto?> ListOneAsync(int tuitionId) => await _db.Tuitions.AsNoTracking()
        .Where(item => item.TuitionId == tuitionId)
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
        }).FirstOrDefaultAsync();

    private async Task<decimal> GetRateAsync()
    {
        var setting = await _db.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(item => item.SettingKey == "TUITION_PER_SESSION");
        return decimal.TryParse(setting?.SettingValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)
            && rate > 0 ? rate : DefaultRate;
    }

    private static string? ValidatePeriod(DateOnly from, DateOnly to) => from == default || to == default || to < from
        ? "Khoảng kỳ học phí không hợp lệ."
        : to.DayNumber - from.DayNumber > 366 ? "Khoảng xem học phí không được vượt quá một năm." : null;
}
