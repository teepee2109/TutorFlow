using Microsoft.EntityFrameworkCore;
using TutorFlow.Data.Models.Keyless;

namespace TutorFlow.Data.Entities;

public partial class TutorFlowDbContext
{
    public virtual DbSet<RoomShiftStatusDto> RoomShiftStatuses { get; set; }
    public virtual DbSet<StudentAttendanceTuitionDto> StudentAttendanceTuitions { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // 1. Khai báo BẮT BUỘC cho EF Core 7+ khi bảng có database triggers
        modelBuilder.Entity<Facility>().ToTable(t => t.HasTrigger("trg_Facilities_Validate"));
        modelBuilder.Entity<Room>().ToTable(t => t.HasTrigger("trg_Rooms_ClosedHasFutureSession"));
        modelBuilder.Entity<Class>().ToTable(t => t.HasTrigger("trg_Classes_Validate"));
        modelBuilder.Entity<ClassSession>().ToTable(t => t.HasTrigger("trg_ClassSessions_Validate"));
        modelBuilder.Entity<Enrollment>().ToTable(t => t.HasTrigger("trg_Enrollments_Validate"));
        modelBuilder.Entity<Attendance>().ToTable(t => t.HasTrigger("trg_Attendance_Validate"));

        // 2. Global Query Filter: Mặc định ẩn người dùng đã xóa mềm (BR-09)
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        // 3. Khai báo Keyless Entity Types cho các Table-Valued Functions (TVF)
        modelBuilder.Entity<RoomShiftStatusDto>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });

        modelBuilder.Entity<StudentAttendanceTuitionDto>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });
    }

    /// <summary>
    /// Helper gọi TVF fn_RoomShiftStatus (F6)
    /// </summary>
    public IQueryable<RoomShiftStatusDto> GetRoomShiftStatus(DateOnly targetDate)
    {
        return Set<RoomShiftStatusDto>()
            .FromSqlInterpolated($"SELECT * FROM dbo.fn_RoomShiftStatus({targetDate})");
    }

    /// <summary>
    /// Helper gọi TVF fn_StudentAttendanceTuition (F5)
    /// </summary>
    public IQueryable<StudentAttendanceTuitionDto> GetStudentAttendanceTuition(DateOnly fromDate, DateOnly toDate)
    {
        return Set<StudentAttendanceTuitionDto>()
            .FromSqlInterpolated($"SELECT * FROM dbo.fn_StudentAttendanceTuition({fromDate}, {toDate})");
    }
}
