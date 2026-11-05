using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TutorFlow.Services.Common;

/// <summary>
/// Bộ chuyển đổi lỗi SQL Server thành lỗi nghiệp vụ (ScheduleError)
/// Thiết kế tách hàm Map(int, string) để có thể Unit Test dễ dàng
/// </summary>
public static class SqlExceptionMapper
{
    public const int SqlErrorDeadlock = 1205;
    public const int SqlErrorUniqueConstraint = 2627;
    public const int SqlErrorUniqueIndex = 2601;

    /// <summary>
    /// Ánh xạ dựa trên mã lỗi và thông điệp lỗi của SQL Server
    /// </summary>
    public static (ScheduleError Error, string Message)? Map(int number, string message) => number switch
    {
        // 1. Xung đột Unique Index / Constraint
        SqlErrorUniqueIndex or SqlErrorUniqueConstraint when message.Contains("UX_Sessions_Room_Shift_Date", StringComparison.OrdinalIgnoreCase)
            => (ScheduleError.RoomConflict, "Phòng đã có lớp học khác trong cùng ca và ngày này."),

        SqlErrorUniqueIndex or SqlErrorUniqueConstraint when message.Contains("UX_Sessions_Lecturer_Shift_Date", StringComparison.OrdinalIgnoreCase)
            => (ScheduleError.LecturerConflict, "Giảng viên đã có lịch dạy lớp khác trong cùng ca và ngày này."),

        SqlErrorUniqueIndex or SqlErrorUniqueConstraint when message.Contains("UQ_Session", StringComparison.OrdinalIgnoreCase)
            => (ScheduleError.SessionDuplicate, "Lớp đã có buổi học được xếp trong ngày này."),

        SqlErrorUniqueIndex or SqlErrorUniqueConstraint when message.Contains("UX_Facilities_Manager", StringComparison.OrdinalIgnoreCase)
            => (ScheduleError.InvalidRole, "Mỗi Facility Manager chỉ được phụ trách tối đa 1 cơ sở."),

        SqlErrorUniqueIndex or SqlErrorUniqueConstraint when message.Contains("UQ_Tuitions_Period", StringComparison.OrdinalIgnoreCase)
            => (ScheduleError.ValidationError, "Học phí của học viên cho lớp và kỳ này đã được phát hành trước đó."),

        SqlErrorUniqueIndex or SqlErrorUniqueConstraint when message.Contains("UQ_Enrollment", StringComparison.OrdinalIgnoreCase)
            => (ScheduleError.ValidationError, "Học viên đã ghi danh vào lớp này rồi."),

        // 2. Trigger Custom Error Numbers (50010 - 50017)
        50010 => (ScheduleError.RoomClosed, "Phòng đang đóng bảo trì (Closed), không thể xếp lịch hoặc gán phòng."),
        50011 => (ScheduleError.RoomHasSchedule, "Phòng vẫn còn buổi học sắp tới (Scheduled). Hãy dời hoặc hủy các buổi đó trước khi đóng phòng."),
        50012 => (ScheduleError.StudentSessionConflict, "Buổi học này bị trùng ca và ngày với buổi học của lớp khác mà học viên đã ghi danh."),
        50013 => (ScheduleError.StudentEnrollConflict, "Học viên đã có lớp học khác cùng ca và ngày trong khoảng thời gian này."),
        50014 => (ScheduleError.InvalidRole, "Tài khoản không đúng vai trò được phép cho thao tác này."),
        50015 => (ScheduleError.AttendanceNotEnrolled, "Học viên chưa ghi danh vào lớp của buổi học này."),
        50016 => (ScheduleError.AttendanceForbidden, "Chỉ giảng viên được phân công phụ trách buổi học mới có quyền điểm danh."),
        50017 => (ScheduleError.AttendanceSessionInvalid, "Không thể điểm danh buổi học đã hủy hoặc buổi học chưa diễn ra."),

        _ => null
    };

    /// <summary>
    /// Trích xuất SqlException từ DbUpdateException hoặc Exception thông thường
    /// </summary>
    public static (ScheduleError Error, string Message)? Map(Exception ex)
    {
        var sqlEx = FindSqlException(ex);
        if (sqlEx == null)
            return null;

        return Map(sqlEx.Number, sqlEx.Message);
    }

    /// <summary>
    /// Kiểm tra lỗi có phải do Deadlock (1205) gây ra không
    /// </summary>
    public static bool IsDeadlock(Exception ex)
    {
        var sqlEx = FindSqlException(ex);
        return sqlEx?.Number == SqlErrorDeadlock;
    }

    /// <summary>
    /// Tìm SqlException trong chuỗi InnerException
    /// </summary>
    public static SqlException? FindSqlException(Exception? ex)
    {
        var current = ex;
        while (current != null)
        {
            if (current is SqlException sqlEx)
                return sqlEx;
            current = current.InnerException;
        }
        return null;
    }

    /// <summary>
    /// Thực thi một hành động và tự động retry tối đa maxRetries lần nếu gặp Deadlock (1205)
    /// Lưu ý: Mỗi lần retry cần làm mới ChangeTracker của DbContext!
    /// </summary>
    public static async Task<Result<T>> ExecuteWithDeadlockRetryAsync<T>(
        Func<Task<Result<T>>> action,
        Action? onRetry = null,
        int maxRetries = 3,
        int baseDelayMs = 100)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsDeadlock(ex) && attempt < maxRetries)
            {
                onRetry?.Invoke();
                await Task.Delay(baseDelayMs * attempt);
            }
            catch (Exception ex)
            {
                var mapped = Map(ex);
                if (mapped.HasValue)
                {
                    return Result<T>.Fail(mapped.Value.Error, mapped.Value.Message);
                }
                throw;
            }
        }

        return Result<T>.Fail(ScheduleError.ValidationError, "Hệ thống đang bận do xung đột giao dịch (Deadlock). Vui lòng thử lại.");
    }
}
