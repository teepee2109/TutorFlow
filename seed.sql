/*
    TutorFlow demo data seed
    Run this script after the TutorFlow schema has been created in SQL Server.
    This file only inserts missing sample rows; it does not drop or recreate tables.
    It is safe to run more than once.

    The file is UTF-8. When using sqlcmd, run with: sqlcmd -f 65001 -i .\seed.sql -b

    Demo account password: Dev@12345
*/
USE TutorFlow;
GO

IF UNICODE(SUBSTRING(N'Lớp', 2, 1)) <> 7899
BEGIN
    THROW 51005, 'seed.sql must be read as UTF-8. Run sqlcmd with -f 65001.', 1;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Facilities', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Classes', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ClassSessions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Enrollments', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Attendance', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Tuitions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Payments', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RoomChangeRequests', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Roles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Shifts', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Subjects', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    THROW 51001, N'Chưa có đủ bảng TutorFlow. Hãy tạo schema trước khi chạy seed.sql.', 1;
END;

DECLARE @Today DATE = CONVERT(DATE, GETDATE());
DECLARE @MonthStart DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
DECLARE @TuitionPeriodFrom DATE = DATEADD(MONTH, -1, @MonthStart);
DECLARE @TuitionPeriodTo DATE = DATEADD(DAY, -1, @MonthStart);
DECLARE @DemoPasswordHash NVARCHAR(255) = N'$2a$11$9a.BKnjMG.j5tmrIiZdSc..d0z/Yg8WPLN9vd1SQRkjL9ld.URIey';
DECLARE @AdminRoleId INT;
DECLARE @GrandManagerRoleId INT;
DECLARE @FacilityManagerRoleId INT;
DECLARE @LecturerRoleId INT;
DECLARE @StudentRoleId INT;
DECLARE @GrandManagerId INT;
DECLARE @TuitionPerSession DECIMAL(12, 0);
DECLARE @i INT = 1;

DECLARE @DemoRows TABLE (RowNumber INT NOT NULL PRIMARY KEY);
INSERT INTO @DemoRows (RowNumber)
VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
       (11),(12),(13),(14),(15),(16),(17),(18),(19),(20);

DECLARE @ClassStudentSlots TABLE (SlotNumber INT NOT NULL PRIMARY KEY);
INSERT INTO @ClassStudentSlots (SlotNumber)
VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15);

DECLARE @DateOffsets TABLE (OffsetDays INT NOT NULL PRIMARY KEY);
WHILE @i <= 365
BEGIN
    INSERT INTO @DateOffsets (OffsetDays) VALUES (@i);
    SET @i += 1;
END;
INSERT INTO @DateOffsets (OffsetDays) VALUES (0);

BEGIN TRY
    BEGIN TRANSACTION;

    /* Fixed lookup values: add only missing entries. */
    INSERT INTO dbo.Roles (RoleName)
    SELECT expected.RoleName
    FROM (VALUES (N'Admin'), (N'GrandManager'), (N'FacilityManager'), (N'Lecturer'), (N'Student')) AS expected(RoleName)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles currentRole WHERE currentRole.RoleName = expected.RoleName);

    INSERT INTO dbo.Shifts (ShiftName, StartTime, EndTime)
    SELECT expected.ShiftName, expected.StartTime, expected.EndTime
    FROM (VALUES
        (N'Ca 1', CONVERT(TIME, '16:30:00'), CONVERT(TIME, '18:00:00')),
        (N'Ca 2', CONVERT(TIME, '18:00:00'), CONVERT(TIME, '19:30:00')),
        (N'Ca 3', CONVERT(TIME, '19:30:00'), CONVERT(TIME, '21:00:00'))
    ) AS expected(ShiftName, StartTime, EndTime)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Shifts currentShift WHERE currentShift.ShiftName = expected.ShiftName);

    /* Correct existing demo shifts too; preserve ShiftId references from classes and sessions. */
    UPDATE currentShift
    SET currentShift.StartTime = expected.StartTime,
        currentShift.EndTime = expected.EndTime
    FROM dbo.Shifts AS currentShift
    JOIN (VALUES
        (N'Ca 1', CONVERT(TIME, '16:30:00'), CONVERT(TIME, '18:00:00')),
        (N'Ca 2', CONVERT(TIME, '18:00:00'), CONVERT(TIME, '19:30:00')),
        (N'Ca 3', CONVERT(TIME, '19:30:00'), CONVERT(TIME, '21:00:00'))
    ) AS expected(ShiftName, StartTime, EndTime)
        ON expected.ShiftName = currentShift.ShiftName;

    INSERT INTO dbo.Subjects (SubjectName)
    SELECT expected.SubjectName
    FROM (VALUES (N'Math'), (N'Literature'), (N'Physics'), (N'English'), (N'Chemistry')) AS expected(SubjectName)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects currentSubject WHERE currentSubject.SubjectName = expected.SubjectName);

    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'TUITION_PER_SESSION')
    BEGIN
        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description)
        VALUES (N'TUITION_PER_SESSION', N'200000', N'Học phí mặc định mỗi buổi học (VNĐ)');
    END;

    SELECT
        @AdminRoleId = MAX(CASE WHEN RoleName = N'Admin' THEN RoleId END),
        @GrandManagerRoleId = MAX(CASE WHEN RoleName = N'GrandManager' THEN RoleId END),
        @FacilityManagerRoleId = MAX(CASE WHEN RoleName = N'FacilityManager' THEN RoleId END),
        @LecturerRoleId = MAX(CASE WHEN RoleName = N'Lecturer' THEN RoleId END),
        @StudentRoleId = MAX(CASE WHEN RoleName = N'Student' THEN RoleId END)
    FROM dbo.Roles;

    IF @AdminRoleId IS NULL OR @GrandManagerRoleId IS NULL OR @FacilityManagerRoleId IS NULL
       OR @LecturerRoleId IS NULL OR @StudentRoleId IS NULL
    BEGIN
        THROW 51002, N'Thiếu role cần thiết để tạo tài khoản mẫu.', 1;
    END;

    /* Demo users: admin, grand manager, 20 facility managers, lecturers, and students. */
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, RoleId, IsActive, IsDeleted)
    SELECT N'System Admin', N'admin@tutorflow.local', @DemoPasswordHash, @AdminRoleId, 1, 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@tutorflow.local');

    INSERT INTO dbo.Users (FullName, Email, PasswordHash, RoleId, IsActive, IsDeleted)
    SELECT N'Grand Manager', N'gm@tutorflow.local', @DemoPasswordHash, @GrandManagerRoleId, 1, 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'gm@tutorflow.local');

    INSERT INTO dbo.Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
    SELECT
        account.FullName,
        account.Email,
        @DemoPasswordHash,
        account.PhoneNumber,
        account.RoleId,
        1,
        0
    FROM
    (
        SELECT CONCAT(N'Facility Manager ', demo.RowNumber) AS FullName,
               CONCAT(N'fm', demo.RowNumber, N'@tutorflow.local') AS Email,
               CONCAT(N'090200', RIGHT(CONCAT(N'00', demo.RowNumber), 2)) AS PhoneNumber,
               @FacilityManagerRoleId AS RoleId
        FROM @DemoRows AS demo
        UNION ALL
        SELECT CONCAT(N'Giảng viên mẫu ', demo.RowNumber),
               CONCAT(N'lecturer', demo.RowNumber, N'@tutorflow.local'),
               CONCAT(N'091300', RIGHT(CONCAT(N'00', demo.RowNumber), 2)),
               @LecturerRoleId
        FROM @DemoRows AS demo
        UNION ALL
        SELECT CONCAT(N'Học viên mẫu ', demo.RowNumber),
               CONCAT(N'student', demo.RowNumber, N'@tutorflow.local'),
               CONCAT(N'098400', RIGHT(CONCAT(N'00', demo.RowNumber), 2)),
               @StudentRoleId
        FROM @DemoRows AS demo
    ) AS account
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Users existingUser WHERE existingUser.Email = account.Email);

    /* Create 14 additional demo students for each sample class (15 students per class total). */
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
    SELECT
        CONCAT(N'Học viên lớp ', RIGHT(CONCAT(N'00', demo.RowNumber), 2), N' - ', RIGHT(CONCAT(N'00', slot.SlotNumber), 2)),
        CONCAT(N'student_demo_', RIGHT(CONCAT(N'00', demo.RowNumber), 2), N'_', RIGHT(CONCAT(N'00', slot.SlotNumber), 2), N'@tutorflow.local'),
        @DemoPasswordHash,
        CONCAT(N'0985', RIGHT(CONCAT(N'00', demo.RowNumber), 2), RIGHT(CONCAT(N'00', slot.SlotNumber), 2)),
        @StudentRoleId,
        1,
        0
    FROM @DemoRows AS demo
    JOIN @ClassStudentSlots AS slot ON 1 = 1
    WHERE slot.SlotNumber > 1
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.Users existingUser
          WHERE existingUser.Email = CONCAT(N'student_demo_', RIGHT(CONCAT(N'00', demo.RowNumber), 2), N'_', RIGHT(CONCAT(N'00', slot.SlotNumber), 2), N'@tutorflow.local')
      );

    SELECT @GrandManagerId = UserId
    FROM dbo.Users
    WHERE Email = N'gm@tutorflow.local';

    /* 20 facilities, two open rooms per facility, and one manager per facility where available. */
    INSERT INTO dbo.Facilities (FacilityName, Address)
    SELECT CONCAT(N'Cơ sở ', demo.RowNumber), CONCAT(N'Địa chỉ mẫu cơ sở ', demo.RowNumber, N', TP.HCM')
    FROM @DemoRows AS demo
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Facilities currentFacility
        WHERE currentFacility.FacilityName = CONCAT(N'Cơ sở ', demo.RowNumber)
    );

    UPDATE facility
    SET ManagerId = manager.UserId
    FROM dbo.Facilities AS facility
    JOIN @DemoRows AS demo
      ON facility.FacilityName = CONCAT(N'Cơ sở ', demo.RowNumber)
    JOIN dbo.Users AS manager
      ON manager.Email = CONCAT(N'fm', demo.RowNumber, N'@tutorflow.local')
    WHERE facility.ManagerId IS NULL
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.Facilities otherFacility
          WHERE otherFacility.ManagerId = manager.UserId
      );

    INSERT INTO dbo.Rooms (FacilityId, RoomNumber, Capacity, Condition)
    SELECT facility.FacilityId,
           CONCAT(N'P', RIGHT(CONCAT(N'00', demo.RowNumber), 2), RIGHT(CONCAT(N'00', room.RoomNumber), 2)),
           30,
           N'Open'
    FROM @DemoRows AS demo
    JOIN dbo.Facilities AS facility
      ON facility.FacilityName = CONCAT(N'Cơ sở ', demo.RowNumber)
    CROSS JOIN (VALUES (1), (2)) AS room(RoomNumber)
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Rooms currentRoom
        WHERE currentRoom.FacilityId = facility.FacilityId
          AND currentRoom.RoomNumber = CONCAT(N'P', RIGHT(CONCAT(N'00', demo.RowNumber), 2), RIGHT(CONCAT(N'00', room.RoomNumber), 2))
    );

    /* 20 classes, one per facility. */
    ;WITH RankedSubjects AS
    (
        SELECT SubjectId, ROW_NUMBER() OVER (ORDER BY SubjectId) AS RowNumber
        FROM dbo.Subjects
    ),
    RankedShifts AS
    (
        SELECT ShiftId, ROW_NUMBER() OVER (ORDER BY ShiftId) AS RowNumber
        FROM dbo.Shifts
    )
    INSERT INTO dbo.Classes
        (ClassName, SubjectId, LecturerId, FacilityId, RoomId, ShiftId, DaysOfWeek, StartDate, EndDate, Status, CreatedBy)
    SELECT
        CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2)),
        subject.SubjectId,
        lecturer.UserId,
        facility.FacilityId,
        room.RoomId,
        shift.ShiftId,
        N'2,3,4,5,6,7,8',
        DATEADD(DAY, -60, @Today),
        DATEADD(DAY, 180, @Today),
        N'Active',
        @GrandManagerId
    FROM @DemoRows AS demo
    JOIN dbo.Facilities AS facility
      ON facility.FacilityName = CONCAT(N'Cơ sở ', demo.RowNumber)
    JOIN dbo.Rooms AS room
      ON room.FacilityId = facility.FacilityId
     AND room.RoomNumber = CONCAT(N'P', RIGHT(CONCAT(N'00', demo.RowNumber), 2), N'01')
    JOIN dbo.Users AS lecturer
      ON lecturer.Email = CONCAT(N'lecturer', demo.RowNumber, N'@tutorflow.local')
    JOIN RankedSubjects AS subject
      ON subject.RowNumber = ((demo.RowNumber - 1) % (SELECT COUNT(*) FROM dbo.Subjects)) + 1
    JOIN RankedShifts AS shift
      ON shift.RowNumber = ((demo.RowNumber - 1) % (SELECT COUNT(*) FROM dbo.Shifts)) + 1
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Classes currentClass
        WHERE currentClass.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
    );

    /* Exactly 15 sample students are assigned to each demo class. */
    INSERT INTO dbo.Enrollments (StudentId, ClassId, EnrollDate, Status)
    SELECT student.UserId, class.ClassId, DATEADD(DAY, -60, @Today), N'Active'
    FROM @DemoRows AS demo
    JOIN @ClassStudentSlots AS slot ON 1 = 1
    JOIN dbo.Users AS student
      ON student.Email = CASE
          WHEN slot.SlotNumber = 1
              THEN CONCAT(N'student', demo.RowNumber, N'@tutorflow.local')
          ELSE CONCAT(N'student_demo_', RIGHT(CONCAT(N'00', demo.RowNumber), 2), N'_', RIGHT(CONCAT(N'00', slot.SlotNumber), 2), N'@tutorflow.local')
      END
    JOIN dbo.Classes AS class
      ON class.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Enrollments currentEnrollment
        WHERE currentEnrollment.StudentId = student.UserId
          AND currentEnrollment.ClassId = class.ClassId
    )
      AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.ClassSessions targetSession
        JOIN dbo.Enrollments otherEnrollment
          ON otherEnrollment.StudentId = student.UserId
         AND otherEnrollment.ClassId <> class.ClassId
         AND otherEnrollment.Status = N'Active'
        JOIN dbo.ClassSessions otherSession
          ON otherSession.ClassId = otherEnrollment.ClassId
         AND otherSession.SessionDate = targetSession.SessionDate
         AND otherSession.ShiftId = targetSession.ShiftId
         AND otherSession.Status <> N'Cancelled'
        WHERE targetSession.ClassId = class.ClassId
          AND targetSession.Status <> N'Cancelled'
    );

    /* Fill every other active class to 15 active students using one student account per class. */
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
    SELECT
        CONCAT(N'Học viên bổ sung ', class.ClassId, N' - ', RIGHT(CONCAT(N'00', slot.SlotNumber), 2)),
        CONCAT(N'student_class_', class.ClassId, N'_', slot.SlotNumber, N'@tutorflow.local'),
        @DemoPasswordHash,
        CONCAT(N'0986', RIGHT(CONCAT(N'000000', class.ClassId), 6), RIGHT(CONCAT(N'00', slot.SlotNumber), 2)),
        @StudentRoleId,
        1,
        0
    FROM dbo.Classes AS class
    JOIN @ClassStudentSlots AS slot
      ON slot.SlotNumber >
      (
          SELECT COUNT_BIG(*)
          FROM dbo.Enrollments enrollment
          WHERE enrollment.ClassId = class.ClassId
            AND enrollment.Status = N'Active'
      )
    WHERE class.Status = N'Active'
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.Users existingUser
          WHERE existingUser.Email = CONCAT(N'student_class_', class.ClassId, N'_', slot.SlotNumber, N'@tutorflow.local')
      );

    INSERT INTO dbo.Enrollments (StudentId, ClassId, EnrollDate, Status)
    SELECT student.UserId,
           class.ClassId,
           CASE WHEN class.StartDate > DATEADD(DAY, -60, @Today) THEN class.StartDate ELSE DATEADD(DAY, -60, @Today) END,
           N'Active'
    FROM dbo.Classes AS class
    JOIN @ClassStudentSlots AS slot
      ON slot.SlotNumber >
      (
          SELECT COUNT_BIG(*)
          FROM dbo.Enrollments enrollment
          WHERE enrollment.ClassId = class.ClassId
            AND enrollment.Status = N'Active'
      )
    JOIN dbo.Users AS student
      ON student.Email = CONCAT(N'student_class_', class.ClassId, N'_', slot.SlotNumber, N'@tutorflow.local')
    WHERE class.Status = N'Active'
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.Enrollments currentEnrollment
          WHERE currentEnrollment.StudentId = student.UserId
            AND currentEnrollment.ClassId = class.ClassId
      );

    /* Add a completed session during the current month if the class lacks one. */
    INSERT INTO dbo.ClassSessions (ClassId, SessionDate, RoomId, ShiftId, LecturerId, Status)
    SELECT class.ClassId, candidate.SessionDate, class.RoomId, class.ShiftId, class.LecturerId, N'Completed'
    FROM @DemoRows AS demo
    JOIN dbo.Classes AS class
      ON class.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
    CROSS APPLY
    (
        SELECT TOP (1) DATEADD(DAY, -dateOffset.OffsetDays, @Today) AS SessionDate
        FROM @DateOffsets AS dateOffset
        WHERE DATEADD(DAY, -dateOffset.OffsetDays, @Today) BETWEEN @MonthStart AND @Today
          AND DATEADD(DAY, -dateOffset.OffsetDays, @Today) BETWEEN class.StartDate AND class.EndDate
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ClassSessions sameClassDate
              WHERE sameClassDate.ClassId = class.ClassId
                AND sameClassDate.SessionDate = DATEADD(DAY, -dateOffset.OffsetDays, @Today)
          )
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ClassSessions roomConflict
              WHERE roomConflict.RoomId = class.RoomId
                AND roomConflict.ShiftId = class.ShiftId
                AND roomConflict.SessionDate = DATEADD(DAY, -dateOffset.OffsetDays, @Today)
                AND roomConflict.Status <> N'Cancelled'
          )
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ClassSessions lecturerConflict
              WHERE lecturerConflict.LecturerId = class.LecturerId
                AND lecturerConflict.ShiftId = class.ShiftId
                AND lecturerConflict.SessionDate = DATEADD(DAY, -dateOffset.OffsetDays, @Today)
                AND lecturerConflict.Status <> N'Cancelled'
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Enrollments targetEnrollment
              JOIN dbo.Enrollments otherEnrollment
                ON otherEnrollment.StudentId = targetEnrollment.StudentId
               AND otherEnrollment.ClassId <> targetEnrollment.ClassId
               AND otherEnrollment.Status = N'Active'
              JOIN dbo.ClassSessions otherSession
                ON otherSession.ClassId = otherEnrollment.ClassId
               AND otherSession.SessionDate = DATEADD(DAY, -dateOffset.OffsetDays, @Today)
               AND otherSession.ShiftId = class.ShiftId
               AND otherSession.Status <> N'Cancelled'
              WHERE targetEnrollment.ClassId = class.ClassId
                AND targetEnrollment.Status = N'Active'
          )
        ORDER BY CASE WHEN dateOffset.OffsetDays = 0 THEN 1 ELSE 0 END, dateOffset.OffsetDays
    ) AS candidate
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.ClassSessions currentSession
        WHERE currentSession.ClassId = class.ClassId
          AND currentSession.Status = N'Completed'
          AND currentSession.SessionDate BETWEEN @MonthStart AND @Today
    );

    /* Add one upcoming session per sample class where no future one exists. */
    INSERT INTO dbo.ClassSessions (ClassId, SessionDate, RoomId, ShiftId, LecturerId, Status)
    SELECT class.ClassId, candidate.SessionDate, class.RoomId, class.ShiftId, class.LecturerId, N'Scheduled'
    FROM @DemoRows AS demo
    JOIN dbo.Classes AS class
      ON class.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
    JOIN dbo.Rooms AS sessionRoom
      ON sessionRoom.RoomId = class.RoomId
     AND sessionRoom.Condition = N'Open'
    CROSS APPLY
    (
        SELECT TOP (1) DATEADD(DAY, dateOffset.OffsetDays, @Today) AS SessionDate
        FROM @DateOffsets AS dateOffset
        WHERE dateOffset.OffsetDays > 0
          AND DATEADD(DAY, dateOffset.OffsetDays, @Today) BETWEEN class.StartDate AND class.EndDate
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ClassSessions sameClassDate
              WHERE sameClassDate.ClassId = class.ClassId
                AND sameClassDate.SessionDate = DATEADD(DAY, dateOffset.OffsetDays, @Today)
          )
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ClassSessions roomConflict
              WHERE roomConflict.RoomId = class.RoomId
                AND roomConflict.ShiftId = class.ShiftId
                AND roomConflict.SessionDate = DATEADD(DAY, dateOffset.OffsetDays, @Today)
                AND roomConflict.Status <> N'Cancelled'
          )
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ClassSessions lecturerConflict
              WHERE lecturerConflict.LecturerId = class.LecturerId
                AND lecturerConflict.ShiftId = class.ShiftId
                AND lecturerConflict.SessionDate = DATEADD(DAY, dateOffset.OffsetDays, @Today)
                AND lecturerConflict.Status <> N'Cancelled'
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Enrollments targetEnrollment
              JOIN dbo.Enrollments otherEnrollment
                ON otherEnrollment.StudentId = targetEnrollment.StudentId
               AND otherEnrollment.ClassId <> targetEnrollment.ClassId
               AND otherEnrollment.Status = N'Active'
              JOIN dbo.ClassSessions otherSession
                ON otherSession.ClassId = otherEnrollment.ClassId
               AND otherSession.SessionDate = DATEADD(DAY, dateOffset.OffsetDays, @Today)
               AND otherSession.ShiftId = class.ShiftId
               AND otherSession.Status <> N'Cancelled'
              WHERE targetEnrollment.ClassId = class.ClassId
                AND targetEnrollment.Status = N'Active'
          )
        ORDER BY dateOffset.OffsetDays
    ) AS candidate
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.ClassSessions currentSession
        WHERE currentSession.ClassId = class.ClassId
          AND currentSession.Status = N'Scheduled'
          AND currentSession.SessionDate > @Today
    );

    /* Present attendance for all enrolled active-class students makes tuition visible in the current month. */
    INSERT INTO dbo.Attendance (SessionId, StudentId, Status, Note, RecordedBy)
    SELECT session.SessionId,
           enrollment.StudentId,
           N'Present',
           N'Dữ liệu điểm danh mẫu',
           session.LecturerId
    FROM dbo.Classes AS class
    JOIN dbo.ClassSessions AS session
      ON session.ClassId = class.ClassId
     AND session.Status = N'Completed'
     AND session.SessionDate BETWEEN @MonthStart AND @Today
    JOIN dbo.Enrollments AS enrollment
      ON enrollment.ClassId = class.ClassId
     AND enrollment.Status = N'Active'
     AND enrollment.EnrollDate <= session.SessionDate
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Attendance currentAttendance
        WHERE currentAttendance.SessionId = session.SessionId
          AND currentAttendance.StudentId = enrollment.StudentId
    );

    SELECT @TuitionPerSession = TRY_CONVERT(DECIMAL(12, 0), SettingValue)
    FROM dbo.SystemSettings
    WHERE SettingKey = N'TUITION_PER_SESSION';
    SET @TuitionPerSession = COALESCE(@TuitionPerSession, 200000);

    /* Issue sample invoices for the previous, closed month after completed sessions exist. */
    INSERT INTO dbo.Tuitions
        (StudentId, ClassId, PeriodFrom, PeriodTo, BillableSessions, AmountPerSession, PaymentStatus, DueDate)
    SELECT
        enrollment.StudentId,
        class.ClassId,
        @TuitionPeriodFrom,
        @TuitionPeriodTo,
        attendance.BillableSessions,
        @TuitionPerSession,
        CASE enrollment.StudentId % 3 WHEN 1 THEN N'Paid' WHEN 2 THEN N'PartiallyPaid' ELSE N'Unpaid' END,
        DATEADD(DAY, 7, @Today)
    FROM dbo.Classes AS class
    JOIN dbo.Enrollments AS enrollment
      ON enrollment.ClassId = class.ClassId
     AND enrollment.Status = N'Active'
    OUTER APPLY
    (
        SELECT COUNT(*) AS BillableSessions
        FROM dbo.Attendance AS attendanceRow
        JOIN dbo.ClassSessions AS completedSession
          ON completedSession.SessionId = attendanceRow.SessionId
        WHERE attendanceRow.StudentId = enrollment.StudentId
          AND completedSession.ClassId = class.ClassId
          AND completedSession.Status = N'Completed'
          AND completedSession.SessionDate BETWEEN @TuitionPeriodFrom AND @TuitionPeriodTo
          AND attendanceRow.Status IN (N'Present', N'UnexcusedAbsent')
    ) AS attendance
    WHERE class.Status = N'Active'
      AND attendance.BillableSessions > 0
      AND NOT EXISTS
    (
        SELECT 1 FROM dbo.Tuitions currentTuition
        WHERE currentTuition.StudentId = enrollment.StudentId
          AND currentTuition.ClassId = class.ClassId
          AND currentTuition.PeriodFrom = @TuitionPeriodFrom
          AND currentTuition.PeriodTo = @TuitionPeriodTo
    );

    /* Add a full or partial payment only for sample invoices marked paid. */
    INSERT INTO dbo.Payments (TuitionId, AmountPaid, PaymentDate, PaymentMethod, ReceivedBy, ReceiptNumber)
    SELECT
        tuition.TuitionId,
        CASE WHEN tuition.PaymentStatus = N'Paid' THEN tuition.TotalAmount ELSE tuition.TotalAmount / 2 END,
        CONVERT(DATETIME, @Today),
        CASE WHEN tuition.StudentId % 2 = 1 THEN N'Chuyển khoản' ELSE N'Tiền mặt' END,
        @GrandManagerId,
        CONCAT(N'TF-SEED-', RIGHT(CONCAT(N'0000', tuition.TuitionId), 4))
    FROM dbo.Tuitions AS tuition
    WHERE tuition.PeriodFrom = @TuitionPeriodFrom
      AND tuition.PeriodTo = @TuitionPeriodTo
      AND tuition.PaymentStatus IN (N'Paid', N'PartiallyPaid')
      AND NOT EXISTS (SELECT 1 FROM dbo.Payments existingPayment WHERE existingPayment.TuitionId = tuition.TuitionId);
    /* Room/shift requests with Pending, Approved, and Rejected examples. */
    INSERT INTO dbo.RoomChangeRequests
        (LecturerId, ClassId, SessionId, CurrentRoomId, RequestedRoomId, CurrentShiftId,
         RequestedShiftId, Reason, Status, ReviewedBy, ReviewedAt, ReviewNote)
    SELECT
        class.LecturerId,
        class.ClassId,
        session.SessionId,
        session.RoomId,
        requestedRoom.RoomId,
        session.ShiftId,
        COALESCE(nextShift.ShiftId, firstShift.ShiftId),
        N'Yêu cầu đổi phòng/ca mẫu để kiểm thử',
        CASE demo.RowNumber % 3 WHEN 1 THEN N'Pending' WHEN 2 THEN N'Rejected' ELSE N'Approved' END,
        CASE WHEN demo.RowNumber % 3 = 1 THEN NULL ELSE COALESCE(facility.ManagerId, @GrandManagerId) END,
        CASE WHEN demo.RowNumber % 3 = 1 THEN NULL ELSE GETDATE() END,
        CASE WHEN demo.RowNumber % 3 = 1 THEN NULL ELSE N'Đã xử lý yêu cầu mẫu' END
    FROM @DemoRows AS demo
    JOIN dbo.Classes AS class
      ON class.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
    JOIN dbo.Facilities AS facility
      ON facility.FacilityId = class.FacilityId
    CROSS APPLY
    (
        SELECT TOP (1) scheduledSession.SessionId, scheduledSession.RoomId,
                       scheduledSession.ShiftId, scheduledSession.SessionDate
        FROM dbo.ClassSessions AS scheduledSession
        WHERE scheduledSession.ClassId = class.ClassId
          AND scheduledSession.Status = N'Scheduled'
          AND scheduledSession.SessionDate > @Today
        ORDER BY scheduledSession.SessionDate
    ) AS session
    CROSS APPLY
    (
        SELECT TOP (1) otherRoom.RoomId
        FROM dbo.Rooms AS otherRoom
        WHERE otherRoom.FacilityId = class.FacilityId
          AND otherRoom.RoomId <> session.RoomId
          AND otherRoom.Condition = N'Open'
        ORDER BY otherRoom.RoomId
    ) AS requestedRoom
    OUTER APPLY
    (
        SELECT TOP (1) nextShiftRow.ShiftId
        FROM dbo.Shifts AS nextShiftRow
        WHERE nextShiftRow.ShiftId > session.ShiftId
        ORDER BY nextShiftRow.ShiftId
    ) AS nextShift
    CROSS APPLY
    (
        SELECT MIN(firstShiftRow.ShiftId) AS ShiftId FROM dbo.Shifts AS firstShiftRow
    ) AS firstShift
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.RoomChangeRequests existingRequest
        WHERE existingRequest.ClassId = class.ClassId
    );

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Classes AS sampleClass
        WHERE
        (
            SELECT COUNT_BIG(*)
            FROM dbo.Enrollments enrollment
            WHERE enrollment.ClassId = sampleClass.ClassId
              AND enrollment.Status = N'Active'
        ) < 15
          AND sampleClass.Status = N'Active'
    )
    BEGIN
        THROW 51004, N'Mỗi lớp đang hoạt động phải có ít nhất 15 học viên đang ghi danh.', 1;
    END;

    /* Enforce the lecturer's minimum of 20 rows in each business table. */
    IF (SELECT COUNT_BIG(*) FROM dbo.Users) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Facilities) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Rooms) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Classes) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.ClassSessions) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Enrollments) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Attendance) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Tuitions) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.Payments) < 20
       OR (SELECT COUNT_BIG(*) FROM dbo.RoomChangeRequests) < 20
    BEGIN
        THROW 51003, N'Dữ liệu mẫu chưa đạt tối thiểu 20 bản ghi cho mỗi bảng nghiệp vụ.', 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'Users' AS TableName, COUNT_BIG(*) AS RecordCount FROM dbo.Users
UNION ALL SELECT N'Facilities', COUNT_BIG(*) FROM dbo.Facilities
UNION ALL SELECT N'Rooms', COUNT_BIG(*) FROM dbo.Rooms
UNION ALL SELECT N'Classes', COUNT_BIG(*) FROM dbo.Classes
UNION ALL SELECT N'ClassSessions', COUNT_BIG(*) FROM dbo.ClassSessions
UNION ALL SELECT N'Enrollments', COUNT_BIG(*) FROM dbo.Enrollments
UNION ALL SELECT N'Attendance', COUNT_BIG(*) FROM dbo.Attendance
UNION ALL SELECT N'Tuitions', COUNT_BIG(*) FROM dbo.Tuitions
UNION ALL SELECT N'Payments', COUNT_BIG(*) FROM dbo.Payments
UNION ALL SELECT N'RoomChangeRequests', COUNT_BIG(*) FROM dbo.RoomChangeRequests
ORDER BY TableName;

SELECT sampleClass.ClassName, COUNT_BIG(enrollment.EnrollmentId) AS ActiveStudentCount
FROM dbo.Classes AS sampleClass
LEFT JOIN dbo.Enrollments AS enrollment
  ON enrollment.ClassId = sampleClass.ClassId
 AND enrollment.Status = N'Active'
WHERE sampleClass.Status = N'Active'
GROUP BY sampleClass.ClassName
ORDER BY sampleClass.ClassName;
