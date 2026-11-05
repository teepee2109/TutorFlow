using Microsoft.EntityFrameworkCore;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Common;

namespace TutorFlow.Services.Dashboard;

public interface IAdminDashboardService
{
    Task<Result<VwAdminStat>> GetAdminStatsAsync();
}

public class AdminDashboardService : IAdminDashboardService
{
    private readonly TutorFlowDbContext _db;

    public AdminDashboardService(TutorFlowDbContext db)
    {
        _db = db;
    }

    public async Task<Result<VwAdminStat>> GetAdminStatsAsync()
    {
        try
        {
            var stats = await _db.VwAdminStats.FirstOrDefaultAsync();
            if (stats == null)
            {
                return Result<VwAdminStat>.Fail(ScheduleError.NotFound, "Không tìm thấy dữ liệu thống kê hệ thống.");
            }

            return Result<VwAdminStat>.Ok(stats);
        }
        catch (Exception ex)
        {
            var mapped = SqlExceptionMapper.Map(ex);
            return Result<VwAdminStat>.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? ex.Message);
        }
    }
}
