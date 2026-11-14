using BackEnd.Dtos;
using BackEnd.Services.Common;

namespace BackEnd.Services.Enrollment;

public interface IEnrollmentService
{
    Task<Result<EnrollmentOptionsDto>> GetOptionsAsync();
    Task<Result<List<EnrollmentDto>>> ListAsync(string? search, int? classId, string? status);
    Task<Result<EnrollmentPreviewDto>> PreviewConflictsAsync(EnrollmentRequestDto request);
    Task<Result<BulkEnrollmentCheckDto>> PreviewManyAsync(BulkEnrollmentRequestDto request);
    Task<Result<EnrollmentDto>> EnrollAsync(EnrollmentRequestDto request);
    Task<Result<BulkEnrollmentResultDto>> EnrollManyAsync(BulkEnrollmentRequestDto request);
    Task<Result> DropAsync(int enrollmentId);
}
