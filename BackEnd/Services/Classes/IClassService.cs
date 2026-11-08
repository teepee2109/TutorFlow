using BackEnd.Dtos.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Classes;

public interface IClassService
{
    Task<Result<List<ClassListItemDto>>> GetClassesAsync(string? search = null, string? status = null, int? subjectId = null, int? lecturerId = null);
    Task<Result<ClassDetailDto>> GetClassAsync(int classId);
    Task<Result<ClassOptionsDto>> GetOptionsAsync();
    Task<Result<List<ScheduleConflictDto>>> PreviewConflictsAsync(ClassWriteDto request, int? excludeClassId = null);
    Task<Result<ClassDetailDto>> CreateAsync(ClassWriteDto request, int createdBy);
    Task<Result<ClassDetailDto>> UpdateAsync(int classId, ClassWriteDto request);
    Task<Result> ChangeLecturerAsync(int classId, int lecturerId);
    Task<Result> CloseAsync(int classId);
}
