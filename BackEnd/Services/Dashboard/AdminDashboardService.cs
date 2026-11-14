using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Dtos.Api;
using BackEnd.Services.Common;
using TuitionEntity = BackEnd.Data.Entities.Tuition;

namespace BackEnd.Services.Dashboard;

public interface IAdminDashboardService
{
    Task<Result<AdminDashboardDto>> GetAdminStatsAsync(DateOnly targetDate);
    Task<Result<AdminDashboardDto>> GetFacilityStatsAsync(int facilityId, DateOnly targetDate);
}

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly TutorFlowDbContext _db;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(TutorFlowDbContext db, ILogger<AdminDashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<AdminDashboardDto>> GetAdminStatsAsync(DateOnly targetDate)
    {
        try
        {
            var stats = await _db.VwAdminStats.AsNoTracking().FirstOrDefaultAsync();
            if (stats == null)
            {
                return Result<AdminDashboardDto>.Fail(ScheduleError.NotFound,
                    "Không tìm thấy dữ liệu thống kê hệ thống.");
            }

            var roomShiftStatuses = await GetRoomShiftStatusesAsync(targetDate);
            return Result<AdminDashboardDto>.Ok(new AdminDashboardDto
            {
                TargetDate = targetDate,
                TotalRooms = stats.TotalRooms,
                OpenRooms = stats.OpenRooms,
                ClosedRooms = stats.ClosedRooms,
                ActiveClasses = stats.ActiveClasses,
                ActiveLecturers = stats.ActiveLecturers,
                ActiveStudents = stats.ActiveStudents,
                PendingChangeRequests = stats.PendingChangeRequests,
                TotalUnpaidTuition = await GetUnpaidTuitionBalanceAsync(
                    _db.Tuitions.Where(tuition => tuition.PaymentStatus != "Paid")),
                RoomShiftStatuses = roomShiftStatuses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the system dashboard for {TargetDate}.", targetDate);
            var mapped = SqlExceptionMapper.Map(ex);
            return Result<AdminDashboardDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể tải thống kê lúc này.");
        }
    }

    public async Task<Result<AdminDashboardDto>> GetFacilityStatsAsync(int facilityId, DateOnly targetDate)
    {
        try
        {
            var facility = await _db.Facilities.AsNoTracking()
                .Where(item => item.FacilityId == facilityId)
                .Select(item => new { item.FacilityId, item.FacilityName })
                .FirstOrDefaultAsync();

            if (facility == null)
            {
                return Result<AdminDashboardDto>.Fail(ScheduleError.NotFound,
                    "Không tìm thấy cơ sở được phân công.");
            }

            var facilityRooms = _db.Rooms.Where(room => room.FacilityId == facilityId);
            var activeClasses = _db.Classes.Where(c => c.FacilityId == facilityId && c.Status == "Active");
            var roomShiftStatuses = await _db.RoomShiftStatuses
                .FromSqlInterpolated($"SELECT * FROM dbo.fn_RoomShiftStatus({targetDate})")
                .AsNoTracking()
                .Where(slot => slot.FacilityId == facilityId)
                .ToListAsync();

            var dto = new AdminDashboardDto
            {
                TargetDate = targetDate,
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                TotalRooms = await facilityRooms.CountAsync(),
                OpenRooms = await facilityRooms.CountAsync(room => room.Condition == "Open"),
                ClosedRooms = await facilityRooms.CountAsync(room => room.Condition == "Closed"),
                ActiveClasses = await activeClasses.CountAsync(),
                ActiveLecturers = await activeClasses
                    .Where(c => c.Lecturer.IsActive)
                    .Select(c => c.LecturerId)
                    .Distinct()
                    .CountAsync(),
                ActiveStudents = await _db.Enrollments
                    .Where(enrollment => enrollment.Status == "Active"
                        && enrollment.Class.FacilityId == facilityId
                        && enrollment.Class.Status == "Active"
                        && enrollment.Student.IsActive)
                    .Select(enrollment => enrollment.StudentId)
                    .Distinct()
                    .CountAsync(),
                PendingChangeRequests = await _db.RoomChangeRequests
                    .CountAsync(request => request.Status == "Pending"
                        && ((request.RequestedRoomId.HasValue && request.RequestedRoom!.FacilityId == facilityId)
                            || (!request.RequestedRoomId.HasValue && request.CurrentRoom.FacilityId == facilityId))),
                TotalUnpaidTuition = await GetUnpaidTuitionBalanceAsync(
                    _db.Tuitions.Where(tuition => tuition.PaymentStatus != "Paid"
                        && tuition.Class.FacilityId == facilityId)),
                RoomShiftStatuses = roomShiftStatuses
            };

            return Result<AdminDashboardDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the facility dashboard for facility {FacilityId} and {TargetDate}.",
                facilityId, targetDate);
            var mapped = SqlExceptionMapper.Map(ex);
            return Result<AdminDashboardDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể tải thống kê cơ sở lúc này.");
        }
    }

    private Task<List<Dtos.RoomShiftStatusDto>> GetRoomShiftStatusesAsync(DateOnly targetDate) =>
        _db.RoomShiftStatuses
            .FromSqlInterpolated($"SELECT * FROM dbo.fn_RoomShiftStatus({targetDate})")
            .AsNoTracking()
            .ToListAsync();

    private async Task<decimal> GetUnpaidTuitionBalanceAsync(IQueryable<TuitionEntity> unpaidTuitions)
    {
        // Keep the two aggregates separate; SQL Server rejects SUM(expression containing a subquery).
        var totalAmount = await unpaidTuitions
            .SumAsync(tuition => tuition.TotalAmount ?? 0m);

        var unpaidTuitionIds = unpaidTuitions.Select(tuition => tuition.TuitionId);
        var paidAmount = await _db.Payments
            .Where(payment => unpaidTuitionIds.Contains(payment.TuitionId))
            .SumAsync(payment => (decimal?)payment.AmountPaid) ?? 0m;

        return totalAmount - paidAmount;
    }
}
