using System;
using System.Collections.Generic;
using BackEnd.Data.Entities;
using BackEnd.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data;

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

    public virtual DbSet<RoomShiftStatusDto> RoomShiftStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261CB92A9B0C");

            entity.ToTable("Attendance", tb => tb.HasTrigger("trg_Attendance_Validate"));

            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.AttendanceRecordedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Recorder");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Sessions");

            entity.HasOne(d => d.Student).WithMany(p => p.AttendanceStudents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Students");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.AttendanceUpdatedByNavigations).HasConstraintName("FK_Attendance_Updater");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C012E55065");

            entity.ToTable(tb => tb.HasTrigger("trg_Classes_Validate"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ClassCreatedByNavigations).HasConstraintName("FK_Classes_CreatedBy");

            entity.HasOne(d => d.Facility).WithMany(p => p.Classes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Facilities");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.ClassLecturers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Lecturer");

            entity.HasOne(d => d.Shift).WithMany(p => p.Classes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Shifts");

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
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
            entity.HasKey(e => e.SessionId).HasName("PK__ClassSes__C9F49290AB3F58F7");

            entity.ToTable(tb => tb.HasTrigger("trg_ClassSessions_Validate"));

            entity.HasIndex(e => new { e.LecturerId, e.ShiftId, e.SessionDate }, "UX_Sessions_Lecturer_Shift_Date")
                .IsUnique()
                .HasFilter("([Status]<>'Cancelled')");

            entity.HasIndex(e => new { e.RoomId, e.ShiftId, e.SessionDate }, "UX_Sessions_Room_Shift_Date")
                .IsUnique()
                .HasFilter("([Status]<>'Cancelled')");

            entity.Property(e => e.Status).HasDefaultValue("Scheduled");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Classes");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.ClassSessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Lecturer");

            entity.HasOne(d => d.Room).WithMany(p => p.ClassSessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Rooms");

            entity.HasOne(d => d.Shift).WithMany(p => p.ClassSessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Shifts");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F68771B856DAA5F");

            entity.ToTable(tb => tb.HasTrigger("trg_Enrollments_Validate"));

            entity.Property(e => e.EnrollDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Students");
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Faciliti__5FB08A7455380812");

            entity.ToTable(tb => tb.HasTrigger("trg_Facilities_Validate"));

            entity.HasIndex(e => e.ManagerId, "UX_Facilities_Manager")
                .IsUnique()
                .HasFilter("([ManagerId] IS NOT NULL)");

            entity.HasOne(d => d.Manager).WithOne(p => p.Facility).HasConstraintName("FK_Facilities_Manager");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38D2057A73");

            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.Payments).HasConstraintName("FK_Payments_Users");

            entity.HasOne(d => d.Tuition).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Tuitions");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A86348AA2");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939B718CD70");

            entity.ToTable(tb => tb.HasTrigger("trg_Rooms_ClosedHasFutureSession"));

            entity.Property(e => e.Condition).HasDefaultValue("Open");

            entity.HasOne(d => d.Facility).WithMany(p => p.Rooms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Facilities");
        });

        modelBuilder.Entity<RoomChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__RoomChan__33A8517AD1EF95F1");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Class).WithMany(p => p.RoomChangeRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_Classes");

            entity.HasOne(d => d.CurrentRoom).WithMany(p => p.RoomChangeRequestCurrentRooms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_CurrentRoom");

            entity.HasOne(d => d.CurrentShift).WithMany(p => p.RoomChangeRequestCurrentShifts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_CurrentShift");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.RoomChangeRequestLecturers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RCR_Lecturer");

            entity.HasOne(d => d.RequestedRoom).WithMany(p => p.RoomChangeRequestRequestedRooms).HasConstraintName("FK_RCR_ReqRoom");

            entity.HasOne(d => d.RequestedShift).WithMany(p => p.RoomChangeRequestRequestedShifts).HasConstraintName("FK_RCR_ReqShift");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RoomChangeRequestReviewedByNavigations).HasConstraintName("FK_RCR_ReviewedBy");

            entity.HasOne(d => d.Session).WithMany(p => p.RoomChangeRequests).HasConstraintName("FK_RCR_Sessions");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__Shifts__C0A8388187D680FF");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA3A8ED473215");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__SystemSe__01E719AC55411404");
        });

        modelBuilder.Entity<Tuition>(entity =>
        {
            entity.HasKey(e => e.TuitionId).HasName("PK__Tuitions__F3CBCB482CC31EB5");

            entity.Property(e => e.AmountPerSession).HasDefaultValue(200000m);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentStatus).HasDefaultValue("Unpaid");
            entity.Property(e => e.TotalAmount).HasComputedColumnSql("([SessionsAttended]*[AmountPerSession])", true);

            entity.HasOne(d => d.Class).WithMany(p => p.Tuitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tuitions_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Tuitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tuitions_Students");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CCD38C8FE");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");

            // Soft-deleted accounts are hidden by default throughout the application.
            entity.HasQueryFilter(user => !user.IsDeleted);
        });

        modelBuilder.Entity<VwAdminStat>(entity =>
        {
            entity.ToView("vw_AdminStats");
        });

        modelBuilder.Entity<RoomShiftStatusDto>(entity =>
        {
            entity.HasNoKey();
            entity.ToView((string?)null);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
