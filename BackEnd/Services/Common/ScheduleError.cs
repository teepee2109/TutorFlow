namespace TutorFlow.Services.Common;

/// <summary>
/// Các mã lỗi nghiệp vụ chuẩn theo tài liệu FINAL_PROJECT_DOC v2.0
/// Ánh xạ từ các ràng buộc và trigger của Database TutorFlow
/// </summary>
public enum ScheduleError
{
    /// <summary>
    /// BR-01: Phòng đã có lớp khác trong ca và ngày này (2601/2627: UX_Sessions_Room_Shift_Date)
    /// </summary>
    RoomConflict,

    /// <summary>
    /// BR-02: Giảng viên đã có lịch dạy lớp khác cùng ca và ngày này (2601/2627: UX_Sessions_Lecturer_Shift_Date)
    /// </summary>
    LecturerConflict,

    /// <summary>
    /// BR-10: Lớp đã có buổi học trong ngày này (2601/2627: UQ_Session)
    /// </summary>
    SessionDuplicate,

    /// <summary>
    /// BR-04: Phòng đang đóng bảo trì (Closed) (50010: ROOM_CLOSED)
    /// </summary>
    RoomClosed,

    /// <summary>
    /// BR-04: Phòng còn buổi học sắp tới chưa xử lý (50011: ROOM_HAS_SCHEDULE)
    /// </summary>
    RoomHasSchedule,

    /// <summary>
    /// BR-03(b): Buổi học mới/dời lịch làm trùng lịch của học viên trong lớp (50012: CONFLICT_STUDENT_SESSION)
    /// </summary>
    StudentSessionConflict,

    /// <summary>
    /// BR-03(a): Học viên đã có lớp học khác cùng ca/ngày khi ghi danh (50013: CONFLICT_STUDENT_ENROLL)
    /// </summary>
    StudentEnrollConflict,

    /// <summary>
    /// BR-06: Người dùng không đúng vai trò được phép (50014: INVALID_ROLE)
    /// </summary>
    InvalidRole,

    /// <summary>
    /// BR-07: Học viên chưa ghi danh vào lớp của buổi học (50015: ATTENDANCE_NOT_ENROLLED)
    /// </summary>
    AttendanceNotEnrolled,

    /// <summary>
    /// BR-07: Chỉ giảng viên của buổi học mới được điểm danh/sửa điểm danh (50016: ATTENDANCE_FORBIDDEN)
    /// </summary>
    AttendanceForbidden,

    /// <summary>
    /// BR-07: Buổi học đã bị hủy hoặc chưa diễn ra (50017: ATTENDANCE_SESSION_INVALID)
    /// </summary>
    AttendanceSessionInvalid,

    /// <summary>
    /// Lỗi chung / validation khác
    /// </summary>
    ValidationError,

    /// <summary>
    /// Không tìm thấy đối tượng
    /// </summary>
    NotFound
}
