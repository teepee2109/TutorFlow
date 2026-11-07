using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Dtos.Lecturer;
using BackEnd.Services.Common;

namespace BackEnd.Services.Lecturer;

public sealed class LecturerScheduleService : ILecturerScheduleService
{
    private readonly TutorFlowDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LecturerScheduleService(TutorFlowDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<LecturerScheduleSessionDto>>> GetMyScheduleAsync(int lecturerId, DateOnly from, DateOnly to)
    {
        if (!IsCurrentLecturer(lecturerId))
        {
            return Result<List<LecturerScheduleSessionDto>>.Fail(
                ScheduleError.AttendanceForbidden, "Chỉ giảng viên đang đăng nhập được xem lịch dạy của mình.");
        }
        if (from == default || to == default || to < from || to.DayNumber - from.DayNumber > 92)
        {
            return Result<List<LecturerScheduleSessionDto>>.Fail(
                ScheduleError.ValidationError, "Khoảng ngày xem lịch không hợp lệ (tối đa 93 ngày).");
        }

        var sessions = await _db.ClassSessions.AsNoTracking()
            .Where(session => session.LecturerId == lecturerId
                && session.SessionDate >= from && session.SessionDate <= to)
            .Include(session => session.Class)
            .Include(session => session.Shift)
            .Include(session => session.Room).ThenInclude(room => room.Facility)
            .OrderBy(session => session.SessionDate)
            .ThenBy(session => session.Shift.StartTime)
            .ThenBy(session => session.Class.ClassName)
            .Select(session => new LecturerScheduleSessionDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                ClassName = session.Class.ClassName,
                SessionDate = session.SessionDate,
                ShiftId = session.ShiftId,
                ShiftName = session.Shift.ShiftName,
                StartTime = session.Shift.StartTime,
                EndTime = session.Shift.EndTime,
                RoomId = session.RoomId,
                RoomNumber = session.Room.RoomNumber,
                FacilityName = session.Room.Facility.FacilityName,
                Status = session.Status
            })
            .ToListAsync();

        return Result<List<LecturerScheduleSessionDto>>.Ok(sessions);
    }

    private bool IsCurrentLecturer(int lecturerId) =>
        _currentUser.IsInRole("Lecturer") && _currentUser.UserId == lecturerId;
}
