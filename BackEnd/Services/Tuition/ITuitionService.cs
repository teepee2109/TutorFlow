using BackEnd.Dtos;
using BackEnd.Services.Common;

namespace BackEnd.Services.Tuition;

public interface ITuitionService
{
    Task<Result<List<TuitionPreviewItemDto>>> PreviewAsync(DateOnly from, DateOnly to, int? classId);
    Task<Result<List<TuitionRecordDto>>> ListAsync(DateOnly from, DateOnly to, int? classId);
    Task<Result<TuitionIssueResultDto>> IssueAsync(TuitionIssueRequestDto request);
    Task<Result<TuitionRecordDto>> RecordPaymentAsync(int tuitionId, PaymentCreateDto request, int receiverId);
    Task<Result<List<PaymentRequestDto>>> GetPendingPaymentRequestsAsync();
    Task<Result<PaymentRequestDto>> ReviewPaymentRequestAsync(int requestId, PaymentRequestReviewDto request, int reviewerId);
}
