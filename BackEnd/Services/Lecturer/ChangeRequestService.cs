using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Dtos.Lecturer;
using BackEnd.Dtos.Rooms;
using BackEnd.Services.Common;

namespace BackEnd.Services.Lecturer;

public sealed class ChangeRequestService : IChangeRequestService
{
    private readonly TutorFlowDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ChangeRequestService(TutorFlowDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<LecturerChangeRequestOptionsDto>> GetCreateOptionsAsync(int lecturerId)
    {
        if (!IsCurrentLecturer(lecturerId))
        {
            return Result<LecturerChangeRequestOptionsDto>.Fail(
                ScheduleError.AttendanceForbidden, "Chỉ giảng viên đang đăng nhập được tạo yêu cầu.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var scopes = new List<RoomChangeScopeOptionDto>();
        var classes = await _db.Classes.AsNoTracking()
            .Where(item => item.LecturerId == lecturerId && item.Status == "Active"
                && item.ClassSessions.Any(session => session.Status == "Scheduled" && session.SessionDate >= today))
            .OrderBy(item => item.ClassName)
            .Select(item => new { item.ClassId, item.ClassName })
            .ToListAsync();
        scopes.AddRange(classes.Select(item => new RoomChangeScopeOptionDto
        {
            ScopeType = "Class",
            ScopeId = item.ClassId,
            ClassId = item.ClassId,
            DisplayName = $"Cả lớp · {item.ClassName}"
        }));

        var sessions = await _db.ClassSessions.AsNoTracking()
            .Where(item => item.LecturerId == lecturerId && item.Status == "Scheduled"
                && item.SessionDate >= today && item.Class.Status == "Active")
            .Include(item => item.Class)
            .OrderBy(item => item.SessionDate).ThenBy(item => item.ShiftId)
            .ToListAsync();
        scopes.AddRange(sessions.Select(item => new RoomChangeScopeOptionDto
        {
            ScopeType = "Session",
            ScopeId = item.SessionId,
            ClassId = item.ClassId,
            SessionId = item.SessionId,
            DisplayName = $"Một buổi · {item.Class.ClassName} · {item.SessionDate:dd/MM/yyyy}"
        }));

        var options = new LecturerChangeRequestOptionsDto
        {
            Scopes = scopes,
            Rooms = await _db.Rooms.AsNoTracking()
                .Where(item => item.Condition == "Open")
                .OrderBy(item => item.Facility.FacilityName).ThenBy(item => item.RoomNumber)
                .Select(item => new RoomOptionDto
                {
                    RoomId = item.RoomId,
                    FacilityId = item.FacilityId,
                    FacilityName = item.Facility.FacilityName,
                    RoomNumber = item.RoomNumber,
                    Capacity = item.Capacity
                }).ToListAsync(),
            Shifts = await _db.Shifts.AsNoTracking().OrderBy(item => item.ShiftId)
                .Select(item => new ShiftOptionDto { ShiftId = item.ShiftId, ShiftName = item.ShiftName })
                .ToListAsync()
        };
        return Result<LecturerChangeRequestOptionsDto>.Ok(options);
    }

    public async Task<Result<LecturerRoomChangeRequestDto>> CreateAsync(
        LecturerChangeRequestCreateDto request,
        int lecturerId)
    {
        if (!IsCurrentLecturer(lecturerId))
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(
                ScheduleError.AttendanceForbidden, "Chỉ giảng viên đang đăng nhập được tạo yêu cầu.");
        }
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(
                ScheduleError.ValidationError, "Lý do bắt buộc và không được vượt quá 500 ký tự.");
        }
        if (!request.RequestedRoomId.HasValue && !request.RequestedShiftId.HasValue)
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(
                ScheduleError.ValidationError, "Hãy chọn ít nhất một phòng hoặc ca mới.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        Class classEntity;
        ClassSession? session = null;
        int currentRoomId;
        int currentShiftId;
        if (request.SessionId.HasValue)
        {
            session = await _db.ClassSessions.AsNoTracking()
                .Include(item => item.Class)
                .FirstOrDefaultAsync(item => item.SessionId == request.SessionId.Value);
            if (session == null || session.ClassId != request.ClassId)
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.NotFound, "Không tìm thấy buổi học trong lớp đã chọn.");
            }
            if (session.LecturerId != lecturerId)
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.AttendanceForbidden, "Bạn không được phân công cho buổi học này.");
            }
            if (session.Status != "Scheduled" || session.SessionDate < today || session.Class.Status != "Active")
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.ValidationError, "Chỉ có thể gửi yêu cầu cho buổi Scheduled sắp tới của lớp đang mở.");
            }

            classEntity = session.Class;
            currentRoomId = session.RoomId;
            currentShiftId = session.ShiftId;
        }
        else
        {
            classEntity = await _db.Classes.AsNoTracking()
                .FirstOrDefaultAsync(item => item.ClassId == request.ClassId)
                ?? null!;
            if (classEntity == null)
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.NotFound, "Không tìm thấy lớp học.");
            }
            if (classEntity.LecturerId != lecturerId)
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.AttendanceForbidden, "Chỉ giảng viên phụ trách lớp được gửi yêu cầu cho cả lớp.");
            }
            if (classEntity.Status != "Active"
                || !await _db.ClassSessions.AnyAsync(item => item.ClassId == request.ClassId
                    && item.Status == "Scheduled" && item.SessionDate >= today))
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.ValidationError, "Lớp không còn buổi Scheduled sắp tới để yêu cầu thay đổi.");
            }

            currentRoomId = classEntity.RoomId;
            currentShiftId = classEntity.ShiftId;
        }

        Room? requestedRoom = null;
        if (request.RequestedRoomId.HasValue)
        {
            requestedRoom = await _db.Rooms.AsNoTracking()
                .FirstOrDefaultAsync(item => item.RoomId == request.RequestedRoomId.Value);
            if (requestedRoom == null)
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.NotFound, "Không tìm thấy phòng được yêu cầu.");
            }
            if (requestedRoom.Condition != "Open")
            {
                return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.RoomClosed, "Phòng được yêu cầu đang đóng bảo trì.");
            }
        }
        if (request.RequestedShiftId.HasValue
            && !await _db.Shifts.AnyAsync(item => item.ShiftId == request.RequestedShiftId.Value))
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.NotFound, "Không tìm thấy ca học được yêu cầu.");
        }
        if (!request.SessionId.HasValue && requestedRoom != null && requestedRoom.FacilityId != classEntity.FacilityId)
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.ValidationError,
                "Phòng mặc định của cả lớp phải thuộc cơ sở của lớp. Hãy chọn yêu cầu đổi cho một buổi nếu cần phòng ở cơ sở khác.");
        }

        if ((request.RequestedRoomId ?? currentRoomId) == currentRoomId
            && (request.RequestedShiftId ?? currentShiftId) == currentShiftId)
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.ValidationError, "Thông tin yêu cầu không tạo ra thay đổi nào.");
        }

        var duplicatePending = await _db.RoomChangeRequests.AnyAsync(item =>
            item.LecturerId == lecturerId && item.ClassId == request.ClassId
            && item.SessionId == request.SessionId && item.Status == "Pending");
        if (duplicatePending)
        {
            return Result<LecturerRoomChangeRequestDto>.Fail(ScheduleError.ValidationError,
                "Đã có một yêu cầu Pending cho phạm vi này. Hãy chờ Facility Manager xử lý.");
        }

        var entity = new RoomChangeRequest
        {
            LecturerId = lecturerId,
            ClassId = request.ClassId,
            SessionId = request.SessionId,
            CurrentRoomId = currentRoomId,
            RequestedRoomId = request.RequestedRoomId,
            CurrentShiftId = currentShiftId,
            RequestedShiftId = request.RequestedShiftId,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        try
        {
            _db.RoomChangeRequests.Add(entity);
            await _db.SaveChangesAsync();
            var created = await GetMineQuery(lecturerId).FirstAsync(item => item.RequestId == entity.RequestId);
            return Result<LecturerRoomChangeRequestDto>.Ok(ToDto(created), "Đã gửi yêu cầu đến Facility Manager.");
        }
        catch (Exception exception)
        {
            _db.ChangeTracker.Clear();
            var mapped = SqlExceptionMapper.Map(exception);
            return Result<LecturerRoomChangeRequestDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError,
                mapped?.Message ?? "Không thể gửi yêu cầu lúc này.");
        }
    }

    public async Task<Result<List<LecturerRoomChangeRequestDto>>> ListMineAsync(int lecturerId)
    {
        if (!IsCurrentLecturer(lecturerId))
        {
            return Result<List<LecturerRoomChangeRequestDto>>.Fail(
                ScheduleError.AttendanceForbidden, "Chỉ giảng viên đang đăng nhập được xem yêu cầu của mình.");
        }

        var requests = await GetMineQuery(lecturerId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        return Result<List<LecturerRoomChangeRequestDto>>.Ok(requests.Select(ToDto).ToList());
    }

    private IQueryable<RoomChangeRequest> GetMineQuery(int lecturerId) => _db.RoomChangeRequests.AsNoTracking()
        .Where(item => item.LecturerId == lecturerId)
        .Include(item => item.Class)
        .Include(item => item.Session)
        .Include(item => item.CurrentRoom)
        .Include(item => item.RequestedRoom)
        .Include(item => item.CurrentShift)
        .Include(item => item.RequestedShift);

    private bool IsCurrentLecturer(int lecturerId) =>
        _currentUser.IsInRole("Lecturer") && _currentUser.UserId == lecturerId;

    private static LecturerRoomChangeRequestDto ToDto(RoomChangeRequest item) => new()
    {
        RequestId = item.RequestId,
        ClassId = item.ClassId,
        SessionId = item.SessionId,
        ClassName = item.Class.ClassName,
        Scope = item.SessionId.HasValue ? "Một buổi" : "Cả lớp",
        SessionDate = item.Session?.SessionDate,
        CurrentRoom = RoomLabel(item.CurrentRoom),
        RequestedRoom = item.RequestedRoom == null ? null : RoomLabel(item.RequestedRoom),
        CurrentShift = item.CurrentShift.ShiftName,
        RequestedShift = item.RequestedShift?.ShiftName,
        Reason = item.Reason,
        Status = item.Status,
        CreatedAt = item.CreatedAt,
        ReviewedAt = item.ReviewedAt,
        ReviewNote = item.ReviewNote
    };

    private static string RoomLabel(Room room) => $"{room.RoomNumber} (Cơ sở {room.FacilityId})";
}
