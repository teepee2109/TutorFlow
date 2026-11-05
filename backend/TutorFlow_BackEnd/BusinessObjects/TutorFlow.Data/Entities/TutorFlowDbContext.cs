using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TutorFlow.Data.Entities;

public partial class TutorFlowDbContext : DbContext
{
    public TutorFlowDbContext(DbContextOptions<TutorFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassSession> ClassSessions { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Facility> Facilities { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomChangeRequest> RoomChangeRequests { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<Tuition> Tuitions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwAdminStat> VwAdminStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261CB4A088E3");

            entity.ToTable("Attendance", tb => tb.HasTrigger("trg_Attendance_Validate"));

            entity.HasIndex(e => e.StudentId, "IX_Attendance_StudentId");

            entity.HasIndex(e => new { e.SessionId, e.StudentId }, "UQ_Attendance").IsUnique();

            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.AttendanceRecordedByNavigations)
                .HasForeignKey(d => d.RecordedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Recorder");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Sessions");

            entity.HasOne(d => d.Student).WithMany(p => p.AttendanceStudents)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Students");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.AttendanceUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Attendance_Updater");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0AF712E36");

            entity.ToTable(tb => tb.HasTrigger("trg_Classes_Validate"));

            entity.HasIndex(e => e.LecturerId, "IX_Classes_LecturerId");

            entity.HasIndex(e => new { e.RoomId, e.ShiftId }, "IX_Classes_RoomShift");

            entity.Property(e => e.ClassName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DaysOfWeek).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ClassCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Classes_CreatedBy");

            entity.HasOne(d => d.Facility).WithMany(p => p.Classes)
                .HasForeignKey(d => d.FacilityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Facilities");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.ClassLecturers)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Lecturer");

            entity.HasOne(d => d.Shift).WithMany(p => p.Classes)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Shifts");

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Subjects");

            entity.HasOne(d => d.Room).WithMany(p => p.Classes)
                .HasPrincipalKey(p => new { p.RoomId, p.FacilityId })
                .HasForeignKey(d => new { d.RoomId, d.FacilityId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Rooms_Facility");
        });

        modelBuilder.Entity<ClassSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__ClassSes__C9F4929037B35393");

            entity.ToTable(tb => tb.HasTrigger("trg_ClassSessions_Validate"));

            entity.HasIndex(e => new { e.ClassId, e.SessionDate }, "IX_Sessions_ClassDate");

            entity.HasIndex(e => new { e.ClassId, e.SessionDate }, "UQ_Session").IsUnique();

            entity.HasIndex(e => new { e.LecturerId, e.ShiftId, e.SessionDate }, "UX_Sessions_Lecturer_Shift_Date")
                .IsUnique()
                .HasFilter("([Status]<>'Cancelled')");

            entity.HasIndex(e => new { e.RoomId, e.ShiftId, e.SessionDate }, "UX_Sessions_Room_Shift_Date")
                .IsUnique()
                .HasFilter("([Status]<>'Cancelled')");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Scheduled");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Classes");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Lecturer");

            entity.HasOne(d => d.Room).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Rooms");

            entity.HasOne(d => d.Shift).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Shifts");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F68771BE21A5880");

            entity.ToTable(tb => tb.HasTrigger("trg_Enrollments_Validate"));

            entity.HasIndex(e => e.ClassId, "IX_Enrollments_ClassId");

            entity.HasIndex(e => new { e.StudentId, e.ClassId }, "UQ_Enrollment").IsUnique();

            entity.Property(e => e.EnrollDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Students");
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Faciliti__5FB08A74C531BD7D");

            entity.ToTable(tb => tb.HasTrigger("trg_Facilities_Validate"));

            entity.HasIndex(e => e.ManagerId, "UX_Facilities_Manager")
                .IsUnique()
                .HasFilter("([ManagerId] IS NOT NULL)");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.FacilityName).HasMaxLength(100);

            entity.HasOne(d => d.Manager).WithOne(p => p.Facility)
                .HasForeignKey<Facility>(d => d.ManagerId)
                .HasConstraintName("FK_Facilities_Manager");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A385B4D798D");

            entity.Property(e => e.AmountPaid).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ReceiptNumber).HasMaxLength(50);

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedBy)
                .HasConstraintName("FK_Payments_Users");

            entity.HasOne(d => d.Tuition).WithMany(p => p.Payments)
                .HasForeignKey(d => d.TuitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Tuitions");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A078FBBF7");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61608A24578A").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__328639397854B1D1");

            entity.ToTable(tb => tb.HasTrigger("trg_Rooms_ClosedHasFutureSession"));

            entity.HasIndex(e => new { e.FacilityId, e.RoomNumber }, "UQ_Room_Facility").IsUnique();

            entity.HasIndex(e => new { e.RoomId, e.FacilityId }, "UQ_Room_Facility_Composite").IsUnique();

            entity.Property(e => e.Condition)
                .HasMaxLength(20)
                .HasDefaultValue("Open");
            entity.Property(e => e.RoomNumber).HasMaxLength(20);

            entity.HasOne(d => d.Facility).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.FacilityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Facilities");
        });

        modelBuilder.Entity<RoomChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__RoomChan__33A8517A852F2767");

            entity.HasIndex(e => e.Status, "IX_RCR_Status");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ReviewNote).HasMaxLength(255);
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Class).WithMany(p => p.RoomChangeRequests)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_Classes");

            entity.HasOne(d => d.CurrentRoom).WithMany(p => p.RoomChangeRequestCurrentRooms)
                .HasForeignKey(d => d.CurrentRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_CurrentRoom");

            entity.HasOne(d => d.CurrentShift).WithMany(p => p.RoomChangeRequestCurrentShifts)
                .HasForeignKey(d => d.CurrentShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_CurrentShift");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.RoomChangeRequestLecturers)
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_Lecturer");

            entity.HasOne(d => d.RequestedRoom).WithMany(p => p.RoomChangeRequestRequestedRooms)
                .HasForeignKey(d => d.RequestedRoomId)
                .HasConstraintName("FK_RCR_ReqRoom");

            entity.HasOne(d => d.RequestedShift).WithMany(p => p.RoomChangeRequestRequestedShifts)
                .HasForeignKey(d => d.RequestedShiftId)
                .HasConstraintName("FK_RCR_ReqShift");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RoomChangeRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_RCR_ReviewedBy");

            entity.HasOne(d => d.Session).WithMany(p => p.RoomChangeRequests)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_RCR_Sessions");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__Shifts__C0A838816A98B0C4");

            entity.Property(e => e.ShiftName).HasMaxLength(50);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA3A8944820F9");

            entity.HasIndex(e => e.SubjectName, "UQ__Subjects__4C5A7D553860B375").IsUnique();

            entity.Property(e => e.SubjectName).HasMaxLength(50);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__SystemSe__01E719AC1DAA3713");

            entity.Property(e => e.SettingKey).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.SettingValue).HasMaxLength(255);
        });

        modelBuilder.Entity<Tuition>(entity =>
        {
            entity.HasKey(e => e.TuitionId).HasName("PK__Tuitions__F3CBCB484891B1BB");

            entity.HasIndex(e => new { e.StudentId, e.PaymentStatus }, "IX_Tuitions_StudentStatus");

            entity.HasIndex(e => new { e.StudentId, e.ClassId, e.PeriodFrom, e.PeriodTo }, "UQ_Tuitions_Period").IsUnique();

            entity.Property(e => e.AmountPerSession)
                .HasDefaultValue(200000m)
                .HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Unpaid");
            entity.Property(e => e.TotalAmount)
                .HasComputedColumnSql("([SessionsAttended]*[AmountPerSession])", true)
                .HasColumnType("decimal(23, 0)");

            entity.HasOne(d => d.Class).WithMany(p => p.Tuitions)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tuitions_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Tuitions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tuitions_Students");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA8FC89BD");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534038FA31E").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<VwAdminStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AdminStats");

            entity.Property(e => e.TotalUnpaidTuition).HasColumnType("decimal(38, 0)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
