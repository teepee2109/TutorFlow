using TutorFlow.Services.Common;
using Xunit;

namespace TutorFlow.Tests;

public class SqlExceptionMapperTests
{
    [Theory]
    [InlineData(2601, "Cannot insert duplicate key row in object 'dbo.ClassSessions' with unique index 'UX_Sessions_Room_Shift_Date'.", ScheduleError.RoomConflict)]
    [InlineData(2627, "Violation of UNIQUE KEY constraint 'UX_Sessions_Room_Shift_Date'.", ScheduleError.RoomConflict)]
    [InlineData(2601, "Cannot insert duplicate key in index 'UX_Sessions_Lecturer_Shift_Date'.", ScheduleError.LecturerConflict)]
    [InlineData(2627, "Violation of UNIQUE KEY constraint 'UQ_Session'.", ScheduleError.SessionDuplicate)]
    [InlineData(2601, "Violation of UNIQUE KEY constraint 'UX_Facilities_Manager'.", ScheduleError.InvalidRole)]
    public void Map_UniqueConstraints_ReturnsExpectedError(int number, string message, ScheduleError expected)
    {
        var result = SqlExceptionMapper.Map(number, message);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Message));
    }

    [Theory]
    [InlineData(50010, "ROOM_CLOSED", ScheduleError.RoomClosed)]
    [InlineData(50011, "ROOM_HAS_SCHEDULE", ScheduleError.RoomHasSchedule)]
    [InlineData(50012, "CONFLICT_STUDENT_SESSION", ScheduleError.StudentSessionConflict)]
    [InlineData(50013, "CONFLICT_STUDENT_ENROLL", ScheduleError.StudentEnrollConflict)]
    [InlineData(50014, "INVALID_ROLE", ScheduleError.InvalidRole)]
    [InlineData(50015, "ATTENDANCE_NOT_ENROLLED", ScheduleError.AttendanceNotEnrolled)]
    [InlineData(50016, "ATTENDANCE_FORBIDDEN", ScheduleError.AttendanceForbidden)]
    [InlineData(50017, "ATTENDANCE_SESSION_INVALID", ScheduleError.AttendanceSessionInvalid)]
    public void Map_TriggerErrorCodes_ReturnsExpectedError(int number, string message, ScheduleError expected)
    {
        var result = SqlExceptionMapper.Map(number, message);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Message));
    }

    [Fact]
    public void Map_UnknownError_ReturnsNull()
    {
        var result = SqlExceptionMapper.Map(99999, "Unknown random error");
        Assert.Null(result);
    }
}
