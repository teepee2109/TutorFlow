using System.Data;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos.Classes;
using BackEnd.Dtos.Rooms;
using BackEnd.Services.Classes;
using BackEnd.Services.Common;

namespace BackEnd.Services.Rooms;

public sealed class ChangeRequestReviewService : IChangeRequestReviewService
{
    private readonly TutorFlowDbContext _db;
    private readonly ISessionService _sessionService;
    private readonly ICurrentUser _currentUser;

    public ChangeRequestReviewService(
        TutorFlowDbContext db,
        ISessionService sessionService,
        ICurrentUser currentUser)
    {
        _db = db;
        _sessionService = sessionService;
        _currentUser = currentUser;
    }

    public async Task<Result<List<RoomChangeReviewDto>>> ListPendingAsync()
    {
        var reviewerCheck = ValidateReviewer();
        if (!reviewerCheck.Success)
        {
            return Result<List<RoomChangeReviewDto>>.Fail(reviewerCheck.Error ?? ScheduleError.InvalidRole, reviewerCheck.Message);
        }

        var requests = await ReviewQuery()
            .Where(item => item.Status == "Pending")
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();

        // Requests for a requested room are reviewed by that room's FM. For a
        // shift-only request, the FM responsible for the current room reviews it.
        var facilityId = _currentUser.FacilityId!.Value;
        var scoped = requests.Where(item =>
            (item.RequestedRoom?.FacilityId ?? item.CurrentRoom.FacilityId) == facilityId);
        return Result<List<RoomChangeReviewDto>>.Ok(scoped.Select(ToDto).ToList());
    }

    public async Task<Result> ApproveAsync(int requestId, string? reviewNote)
    {
        var reviewerCheck = ValidateReviewer();
        if (!reviewerCheck.Success) return reviewerCheck;
        if (reviewNote?.Length > 255)
        {
            return Result.Fail(ScheduleError.ValidationError, "Ghi chú duyệt không được vượt quá 255 ký tự.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var request = await ReviewQuery().FirstOrDefaultAsync(item => item.RequestId == requestId);
            if (request == null || !IsInReviewerFacility(request))
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.NotFound, "Không tìm thấy yêu cầu trong cơ sở được phân công.");
            }
            if (request.Status != "Pending")
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.ValidationError, "Yêu cầu này đã được xử lý trước đó.");
            }
            if (string.IsNullOrWhiteSpace(request.Reason)
                || (request.RequestedRoomId == null && request.RequestedShiftId == null))
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.ValidationError, "Yêu cầu phải có lý do và ít nhất một phòng hoặc ca mới.");
            }
            if (!_currentUser.UserId.HasValue)
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.InvalidRole, "Không xác định được Facility Manager đang đăng nhập.");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var applyToWholeClass = !request.SessionId.HasValue;
            ClassSession? anchorSession;
            int targetRoomId;
            int targetShiftId;
            int targetFacilityId;

            if (applyToWholeClass)
            {
                if (request.Class.Status != "Active"
                    || request.Class.RoomId != request.CurrentRoomId
                    || request.Class.ShiftId != request.CurrentShiftId
                    || request.Class.LecturerId != request.LecturerId)
                {
                    await transaction.RollbackAsync();
                    return Result.Fail(ScheduleError.ValidationError,
                        "Thông tin lớp đã thay đổi kể từ lúc gửi yêu cầu. Hãy yêu cầu giảng viên gửi lại.");
                }

                anchorSession = await _db.ClassSessions
                    .Where(item => item.ClassId == request.ClassId
                        && item.Status == "Scheduled" && item.SessionDate >= today)
                    .OrderBy(item => item.SessionDate)
                    .FirstOrDefaultAsync();
                if (anchorSession == null)
                {
                    await transaction.RollbackAsync();
                    return Result.Fail(ScheduleError.ValidationError, "Lớp không còn buổi Scheduled sắp tới để áp dụng yêu cầu.");
                }

                targetRoomId = request.RequestedRoomId ?? request.Class.RoomId;
                targetShiftId = request.RequestedShiftId ?? request.Class.ShiftId;
                targetFacilityId = request.RequestedRoom?.FacilityId ?? request.Class.FacilityId;
            }
            else
            {
                anchorSession = request.Session;
                if (anchorSession == null || anchorSession.ClassId != request.ClassId
                    || anchorSession.Status != "Scheduled" || anchorSession.SessionDate < today
                    || anchorSession.RoomId != request.CurrentRoomId
                    || anchorSession.ShiftId != request.CurrentShiftId
                    || anchorSession.LecturerId != request.LecturerId)
                {
                    await transaction.RollbackAsync();
                    return Result.Fail(ScheduleError.ValidationError,
                        "Buổi học đã thay đổi hoặc không còn Scheduled. Hãy yêu cầu giảng viên gửi lại.");
                }

                targetRoomId = request.RequestedRoomId ?? anchorSession.RoomId;
                targetShiftId = request.RequestedShiftId ?? anchorSession.ShiftId;
                targetFacilityId = request.RequestedRoom?.FacilityId ?? anchorSession.Room.FacilityId;
            }

            if (targetFacilityId != _currentUser.FacilityId!.Value)
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.InvalidRole, "Yêu cầu này thuộc cơ sở khác.");
            }

            var scheduleResult = await _sessionService.RescheduleSessionAsync(anchorSession.SessionId, new SessionRescheduleDto
            {
                RoomId = targetRoomId,
                ShiftId = targetShiftId,
                ApplyToWholeClass = applyToWholeClass
            });
            if (!scheduleResult.Success)
            {
                await transaction.RollbackAsync();
                _db.ChangeTracker.Clear();
                return scheduleResult;
            }

            request.Status = "Approved";
            request.ReviewedBy = _currentUser.UserId.Value;
            request.ReviewedAt = DateTime.Now;
            request.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok("Đã duyệt yêu cầu và cập nhật lịch học.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể duyệt yêu cầu lúc này.");
        }
    }

    public async Task<Result> RejectAsync(int requestId, string reviewNote)
    {
        var reviewerCheck = ValidateReviewer();
        if (!reviewerCheck.Success) return reviewerCheck;
        if (string.IsNullOrWhiteSpace(reviewNote) || reviewNote.Trim().Length > 255)
        {
            return Result.Fail(ScheduleError.ValidationError, "Lý do từ chối bắt buộc và tối đa 255 ký tự.");
        }
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Fail(ScheduleError.InvalidRole, "Không xác định được Facility Manager đang đăng nhập.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var request = await ReviewQuery().FirstOrDefaultAsync(item => item.RequestId == requestId);
            if (request == null || !IsInReviewerFacility(request))
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.NotFound, "Không tìm thấy yêu cầu trong cơ sở được phân công.");
            }
            if (request.Status != "Pending")
            {
                await transaction.RollbackAsync();
                return Result.Fail(ScheduleError.ValidationError, "Yêu cầu này đã được xử lý trước đó.");
            }

            request.Status = "Rejected";
            request.ReviewedBy = _currentUser.UserId.Value;
            request.ReviewedAt = DateTime.Now;
            request.ReviewNote = reviewNote.Trim();
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Ok("Đã từ chối yêu cầu.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return FailureFromException(exception, "Không thể từ chối yêu cầu lúc này.");
        }
    }

    private IQueryable<RoomChangeRequest> ReviewQuery() => _db.RoomChangeRequests
        .Include(item => item.Class)
        .Include(item => item.Lecturer)
        .Include(item => item.CurrentRoom)
        .Include(item => item.RequestedRoom)
        .Include(item => item.CurrentShift)
        .Include(item => item.RequestedShift)
        .Include(item => item.Session).ThenInclude(item => item!.Room);

    private Result ValidateReviewer()
    {
        if (!_currentUser.IsInRole("FacilityManager") || !_currentUser.FacilityId.HasValue)
        {
            return Result.Fail(ScheduleError.InvalidRole, "Chỉ Facility Manager đã được gán cơ sở mới được xử lý yêu cầu.");
        }
        return Result.Ok();
    }

    private bool IsInReviewerFacility(RoomChangeRequest request) =>
        (request.RequestedRoom?.FacilityId ?? request.CurrentRoom.FacilityId) == _currentUser.FacilityId;

    private static RoomChangeReviewDto ToDto(RoomChangeRequest request) => new()
    {
        RequestId = request.RequestId,
        ClassId = request.ClassId,
        SessionId = request.SessionId,
        ClassName = request.Class.ClassName,
        LecturerName = request.Lecturer.FullName,
        Scope = request.SessionId.HasValue ? "Một buổi" : "Cả lớp",
        SessionDate = request.Session?.SessionDate,
        CurrentRoom = RoomLabel(request.CurrentRoom),
        RequestedRoom = request.RequestedRoom == null ? null : RoomLabel(request.RequestedRoom),
        CurrentShift = request.CurrentShift.ShiftName,
        RequestedShift = request.RequestedShift?.ShiftName,
        Reason = request.Reason,
        CreatedAt = request.CreatedAt
    };

    private static string RoomLabel(Room room) => $"{room.RoomNumber} (Cơ sở {room.FacilityId})";

    private static Result FailureFromException(Exception exception, string fallbackMessage)
    {
        var mapped = SqlExceptionMapper.Map(exception);
        return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? fallbackMessage);
    }
}
