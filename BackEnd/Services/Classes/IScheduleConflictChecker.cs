using BackEnd.Dtos.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Classes;

public interface IScheduleConflictChecker
{
    Task<Result<List<ScheduleConflictDto>>> PreviewClassConflictsAsync(ClassWriteDto request, int? excludeClassId = null);

    Task<Result<List<ScheduleConflictDto>>> PreviewDatesAsync(
        int roomId,
        int shiftId,
        int lecturerId,
        IReadOnlyCollection<DateOnly> dates,
        int? excludeClassId = null,
        IReadOnlyCollection<int>? ignoreSessionIds = null);
}
