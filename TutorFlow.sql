/* =====================================================================
   PROJECT   : TutorFlow (Phiên bản 2.0 - Final Project)
   PLATFORM  : SQL Server (tương thích .NET 8.0 / EF Core 8.0.x)
   MÔ TẢ     : Database cho 5 vai trò - Admin, Grand Manager,
                Facility Manager, Lecturer, Student
   
   DANH SÁCH MÃ LỖI TRIGGER (dùng cho SqlExceptionMapper):
     - 50010 : ROOM_CLOSED              (Phòng đang đóng bảo trì)
     - 50011 : ROOM_HAS_SCHEDULE        (Phòng còn lịch buổi học sắp tới)
     - 50012 : CONFLICT_STUDENT_SESSION (Buổi học làm trùng lịch học viên)
     - 50013 : CONFLICT_STUDENT_ENROLL  (Học viên ghi danh bị trùng lịch)
     - 50014 : INVALID_ROLE             (Sai vai trò tài khoản)
     - 50015 : ATTENDANCE_NOT_ENROLLED  (Học viên chưa ghi danh vào lớp)
     - 50016 : ATTENDANCE_FORBIDDEN     (Chỉ giảng viên buổi học được điểm danh)
     - 50017 : ATTENDANCE_SESSION_INVALID (Buổi bị hủy hoặc chưa diễn ra)
     - 2601/2627:
         * UX_Sessions_Room_Shift_Date     -> ROOM_CONFLICT
         * UX_Sessions_Lecturer_Shift_Date -> LECTURER_CONFLICT
         * UQ_Session                      -> SESSION_DUPLICATE
   ===================================================================== */

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TutorFlow')
BEGIN
    CREATE DATABASE TutorFlow;
END
GO

USE TutorFlow;
GO

-- This file is UTF-8. When using sqlcmd, specify -f 65001 so Vietnamese literals stay intact.
IF UNICODE(SUBSTRING(N'Lớp', 2, 1)) <> 7899
BEGIN
    THROW 51005, 'TutorFlow.sql must be read as UTF-8. Run sqlcmd with -f 65001.', 1;
END;
GO

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ─────────────────────────────────────────────────────────────────────
-- 0. DỌN DẸP NẾU ĐÃ TỒN TẠI ĐỂ TẠO MỚI SẠCH SẼ
-- ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.trg_Attendance_Validate', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Attendance_Validate;
IF OBJECT_ID('dbo.trg_Enrollments_Validate', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Enrollments_Validate;
IF OBJECT_ID('dbo.trg_ClassSessions_Validate', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_ClassSessions_Validate;
IF OBJECT_ID('dbo.trg_Classes_Validate', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Classes_Validate;
IF OBJECT_ID('dbo.trg_Rooms_ClosedHasFutureSession', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Rooms_ClosedHasFutureSession;
IF OBJECT_ID('dbo.trg_Facilities_Validate', 'TR') IS NOT NULL DROP TRIGGER dbo.trg_Facilities_Validate;

IF OBJECT_ID('dbo.vw_AdminStats', 'V') IS NOT NULL DROP VIEW dbo.vw_AdminStats;
IF OBJECT_ID('dbo.fn_RoomShiftStatus', 'IF') IS NOT NULL DROP FUNCTION dbo.fn_RoomShiftStatus;
IF OBJECT_ID('dbo.fn_StudentAttendanceTuition', 'IF') IS NOT NULL DROP FUNCTION dbo.fn_StudentAttendanceTuition;

IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.Tuitions', 'U') IS NOT NULL DROP TABLE dbo.Tuitions;
IF OBJECT_ID('dbo.Attendance', 'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID('dbo.Enrollments', 'U') IS NOT NULL DROP TABLE dbo.Enrollments;
IF OBJECT_ID('dbo.RoomChangeRequests', 'U') IS NOT NULL DROP TABLE dbo.RoomChangeRequests;
IF OBJECT_ID('dbo.ClassSessions', 'U') IS NOT NULL DROP TABLE dbo.ClassSessions;
IF OBJECT_ID('dbo.Classes', 'U') IS NOT NULL DROP TABLE dbo.Classes;
IF OBJECT_ID('dbo.Rooms', 'U') IS NOT NULL DROP TABLE dbo.Rooms;
IF OBJECT_ID('dbo.Facilities', 'U') IS NOT NULL DROP TABLE dbo.Facilities;
IF OBJECT_ID('dbo.Shifts', 'U') IS NOT NULL DROP TABLE dbo.Shifts;
IF OBJECT_ID('dbo.Subjects', 'U') IS NOT NULL DROP TABLE dbo.Subjects;
IF OBJECT_ID('dbo.SystemSettings', 'U') IS NOT NULL DROP TABLE dbo.SystemSettings;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* =====================================================================
   1. ROLES & USERS
   ===================================================================== */

CREATE TABLE Roles (
    RoleId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleName        NVARCHAR(50) NOT NULL UNIQUE
    -- Admin, GrandManager, FacilityManager, Lecturer, Student
);
GO

CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)  NOT NULL,
    Email           NVARCHAR(100)  NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)  NOT NULL,
    PhoneNumber     NVARCHAR(20)   NULL,
    RoleId          INT            NOT NULL,
    IsActive        BIT            NOT NULL DEFAULT 1, -- Khóa/mở tài khoản
    IsDeleted       BIT            NOT NULL DEFAULT 0, -- Xóa mềm (BR-09)
    CreatedAt       DATETIME       NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME       NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

/* =====================================================================
   2. FACILITIES & ROOMS (3 cơ sở x 6 phòng = 18 phòng)
   ===================================================================== */

CREATE TABLE Facilities (
    FacilityId      INT IDENTITY(1,1) PRIMARY KEY,
    FacilityName    NVARCHAR(100) NOT NULL,
    Address         NVARCHAR(255) NULL,
    ManagerId       INT           NULL, -- Facility Manager phụ trách cơ sở
    CONSTRAINT FK_Facilities_Manager FOREIGN KEY (ManagerId) REFERENCES Users(UserId)
);
GO

-- BR-11: Mỗi Facility Manager phụ trách tối đa 1 cơ sở
CREATE UNIQUE INDEX UX_Facilities_Manager
    ON Facilities(ManagerId)
    WHERE ManagerId IS NOT NULL;
GO

CREATE TABLE Rooms (
    RoomId          INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId      INT NOT NULL,
    RoomNumber      NVARCHAR(20) NOT NULL,             -- vd: P101, P102...
    Capacity        INT NULL,
    Condition       NVARCHAR(20) NOT NULL DEFAULT 'Open'
                        CHECK (Condition IN ('Open','Closed')), -- Open / Closed bảo trì
    CONSTRAINT FK_Rooms_Facilities FOREIGN KEY (FacilityId) REFERENCES Facilities(FacilityId),
    CONSTRAINT UQ_Room_Facility UNIQUE (FacilityId, RoomNumber),
    -- Cần Unique trên (RoomId, FacilityId) để làm Composite FK cho Classes (BR-05)
    CONSTRAINT UQ_Room_Facility_Composite UNIQUE (RoomId, FacilityId)
);
GO

/* =====================================================================
   3. SHIFTS (Ca học) & SUBJECTS (Môn học)
   ===================================================================== */

CREATE TABLE Shifts (
    ShiftId         INT IDENTITY(1,1) PRIMARY KEY,
    ShiftName       NVARCHAR(50) NOT NULL,             -- Ca 1, Ca 2, Ca 3
    StartTime       TIME NOT NULL,
    EndTime         TIME NOT NULL
);
GO

CREATE TABLE Subjects (
    SubjectId       INT IDENTITY(1,1) PRIMARY KEY,
    SubjectName     NVARCHAR(50) NOT NULL UNIQUE
    -- Math, Literature, Physics, English, Chemistry
);
GO

/* =====================================================================
   4. CLASSES & CLASS SESSIONS
   ===================================================================== */

CREATE TABLE Classes (
    ClassId         INT IDENTITY(1,1) PRIMARY KEY,
    ClassName       NVARCHAR(100) NOT NULL,
    SubjectId       INT NOT NULL,
    LecturerId      INT NOT NULL,                       -- FK -> Users (Role = Lecturer)
    FacilityId      INT NOT NULL,
    RoomId          INT NOT NULL,                        -- Phòng mặc định
    ShiftId         INT NOT NULL,
    DaysOfWeek      NVARCHAR(20) NOT NULL,               -- vd: "2,4,6" (2=Thứ 2..8=CN)
    StartDate       DATE NOT NULL,
    EndDate         DATE NOT NULL,                       -- BR-12: Bắt buộc có EndDate
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Active'
                        CHECK (Status IN ('Active','Closed')),
    CreatedBy       INT NULL,                            -- Grand Manager tạo
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Classes_Subjects   FOREIGN KEY (SubjectId)  REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_Classes_Lecturer   FOREIGN KEY (LecturerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Classes_Facilities FOREIGN KEY (FacilityId) REFERENCES Facilities(FacilityId),
    CONSTRAINT FK_Classes_Shifts     FOREIGN KEY (ShiftId)    REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_Classes_CreatedBy  FOREIGN KEY (CreatedBy)  REFERENCES Users(UserId),
    -- BR-05: Phòng mặc định của lớp phải thuộc đúng cơ sở của lớp
    CONSTRAINT FK_Classes_Rooms_Facility FOREIGN KEY (RoomId, FacilityId) REFERENCES Rooms(RoomId, FacilityId)
);
GO

CREATE TABLE ClassSessions (
    SessionId       INT IDENTITY(1,1) PRIMARY KEY,
    ClassId         INT NOT NULL,
    SessionDate     DATE NOT NULL,
    RoomId          INT NOT NULL,                        -- Có thể đổi phòng so với phòng gốc của lớp
    ShiftId         INT NOT NULL,
    LecturerId      INT NOT NULL,                        -- Giảng viên dạy buổi này (mặc định theo lớp, cho phép dạy thay)
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Scheduled'
                        CHECK (Status IN ('Scheduled','Completed','Cancelled')),
    CONSTRAINT FK_Sessions_Classes  FOREIGN KEY (ClassId)    REFERENCES Classes(ClassId),
    CONSTRAINT FK_Sessions_Rooms    FOREIGN KEY (RoomId)     REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Sessions_Shifts   FOREIGN KEY (ShiftId)    REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_Sessions_Lecturer FOREIGN KEY (LecturerId) REFERENCES Users(UserId),
    -- BR-10: Mỗi lớp chỉ có tối đa 1 buổi mỗi ngày
    CONSTRAINT UQ_Session UNIQUE (ClassId, SessionDate)
);
GO

-- BR-01: Một phòng chỉ có tối đa 1 buổi học cho mỗi cặp (ngày, ca) (loại trừ Cancelled)
CREATE UNIQUE INDEX UX_Sessions_Room_Shift_Date
    ON ClassSessions (RoomId, ShiftId, SessionDate)
    WHERE Status <> 'Cancelled';
GO

-- BR-02: Giảng viên không dạy 2 lớp cùng ca cùng ngày (kể cả 2 cơ sở khác nhau)
CREATE UNIQUE INDEX UX_Sessions_Lecturer_Shift_Date
    ON ClassSessions (LecturerId, ShiftId, SessionDate)
    WHERE Status <> 'Cancelled';
GO

/* =====================================================================
   5. ENROLLMENTS & ATTENDANCE
   ===================================================================== */

CREATE TABLE Enrollments (
    EnrollmentId    INT IDENTITY(1,1) PRIMARY KEY,
    StudentId       INT NOT NULL,
    ClassId         INT NOT NULL,
    EnrollDate      DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Active'
                        CHECK (Status IN ('Active','Dropped')),
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrollments_Classes  FOREIGN KEY (ClassId)   REFERENCES Classes(ClassId),
    CONSTRAINT UQ_Enrollment UNIQUE (StudentId, ClassId)
);
GO

CREATE TABLE Attendance (
    AttendanceId    INT IDENTITY(1,1) PRIMARY KEY,
    SessionId       INT NOT NULL,
    StudentId       INT NOT NULL,
    Status          NVARCHAR(30) NOT NULL
                        CHECK (Status IN ('Present','ExcusedAbsent','UnexcusedAbsent')),
    Note            NVARCHAR(255) NULL,
    RecordedBy      INT NOT NULL,                        -- Giảng viên thực hiện điểm danh
    RecordedAt      DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedBy       INT NULL,                            -- Giảng viên sửa điểm danh (BR-07)
    UpdatedAt       DATETIME NULL,
    CONSTRAINT FK_Attendance_Sessions FOREIGN KEY (SessionId)  REFERENCES ClassSessions(SessionId),
    CONSTRAINT FK_Attendance_Students FOREIGN KEY (StudentId)  REFERENCES Users(UserId),
    CONSTRAINT FK_Attendance_Recorder FOREIGN KEY (RecordedBy) REFERENCES Users(UserId),
    CONSTRAINT FK_Attendance_Updater  FOREIGN KEY (UpdatedBy)  REFERENCES Users(UserId),
    CONSTRAINT UQ_Attendance UNIQUE (SessionId, StudentId)
);
GO

/* =====================================================================
   6. TUITION & PAYMENTS
   ===================================================================== */

CREATE TABLE Tuitions (
    TuitionId         INT IDENTITY(1,1) PRIMARY KEY,
    StudentId         INT NOT NULL,
    ClassId           INT NOT NULL,
    PeriodFrom        DATE NOT NULL,
    PeriodTo          DATE NOT NULL,
    BillableSessions INT NOT NULL DEFAULT 0,
    AmountPerSession DECIMAL(12,0) NOT NULL DEFAULT 200000,
    TotalAmount AS (BillableSessions * AmountPerSession) PERSISTED,
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Unpaid',
    CONSTRAINT CK_Tuitions_PaymentStatus CHECK (PaymentStatus IN ('Unpaid','PartiallyPaid','Paid')),
    DueDate           DATE NULL,
    CreatedAt         DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Tuitions_Students FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    CONSTRAINT FK_Tuitions_Classes  FOREIGN KEY (ClassId)   REFERENCES Classes(ClassId),
    -- BR-08: Không tạo trùng học phí cùng (học viên, lớp, kỳ)
    CONSTRAINT UQ_Tuitions_Period UNIQUE (StudentId, ClassId, PeriodFrom, PeriodTo)
);
GO

CREATE TABLE Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    TuitionId       INT NOT NULL,
    AmountPaid      DECIMAL(12,0) NOT NULL,
    PaymentDate     DATETIME NOT NULL DEFAULT GETDATE(),
    PaymentMethod   NVARCHAR(50) NULL,                   -- Tiền mặt, Chuyển khoản...
    ReceivedBy      INT NULL,                            -- Grand Manager xác nhận thu
    ReceiptNumber   NVARCHAR(50) NULL,
    CONSTRAINT FK_Payments_Tuitions FOREIGN KEY (TuitionId)  REFERENCES Tuitions(TuitionId),
    CONSTRAINT FK_Payments_Users    FOREIGN KEY (ReceivedBy) REFERENCES Users(UserId)
);
GO

CREATE TABLE PaymentRequests (
    PaymentRequestId INT IDENTITY(1,1) PRIMARY KEY,
    TuitionId INT NOT NULL,
    StudentId INT NOT NULL,
    AmountRequested DECIMAL(12,0) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    TransferReference NVARCHAR(50) NULL,
    Note NVARCHAR(255) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PaymentRequests_Status DEFAULT N'Pending',
    RequestedAt DATETIME NOT NULL CONSTRAINT DF_PaymentRequests_RequestedAt DEFAULT GETDATE(),
    ReviewedBy INT NULL,
    ReviewedAt DATETIME NULL,
    ReviewNote NVARCHAR(255) NULL,
    PaymentId INT NULL,
    CONSTRAINT CK_PaymentRequests_Status_Amount CHECK (Status IN (N'Pending', N'Approved', N'Rejected') AND AmountRequested > 0),
    CONSTRAINT FK_PaymentRequests_Tuitions FOREIGN KEY (TuitionId) REFERENCES Tuitions(TuitionId),
    CONSTRAINT FK_PaymentRequests_Students FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    CONSTRAINT FK_PaymentRequests_Reviewers FOREIGN KEY (ReviewedBy) REFERENCES Users(UserId),
    CONSTRAINT FK_PaymentRequests_Payments FOREIGN KEY (PaymentId) REFERENCES Payments(PaymentId)
);
CREATE INDEX IX_PaymentRequests_TuitionStatus ON PaymentRequests(TuitionId, Status);
CREATE UNIQUE INDEX UX_PaymentRequests_PaymentId ON PaymentRequests(PaymentId) WHERE PaymentId IS NOT NULL;
GO

/* =====================================================================
   7. ROOM / SHIFT CHANGE REQUESTS
   ===================================================================== */

CREATE TABLE RoomChangeRequests (
    RequestId         INT IDENTITY(1,1) PRIMARY KEY,
    LecturerId        INT NOT NULL,
    ClassId           INT NOT NULL,
    SessionId         INT NULL,                          -- NULL = đổi vĩnh viễn cho cả lớp; có giá trị = 1 buổi lẻ
    CurrentRoomId     INT NOT NULL,
    RequestedRoomId   INT NULL,
    CurrentShiftId    INT NOT NULL,
    RequestedShiftId  INT NULL,
    Reason            NVARCHAR(500) NOT NULL,
    Status            NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                          CHECK (Status IN ('Pending','Approved','Rejected')),
    ReviewedBy        INT NULL,                          -- Facility Manager duyệt
    ReviewedAt        DATETIME NULL,
    ReviewNote        NVARCHAR(255) NULL,
    CreatedAt         DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_RCR_Lecturer     FOREIGN KEY (LecturerId)       REFERENCES Users(UserId),
    CONSTRAINT FK_RCR_Classes      FOREIGN KEY (ClassId)          REFERENCES Classes(ClassId),
    CONSTRAINT FK_RCR_Sessions     FOREIGN KEY (SessionId)        REFERENCES ClassSessions(SessionId),
    CONSTRAINT FK_RCR_CurrentRoom  FOREIGN KEY (CurrentRoomId)    REFERENCES Rooms(RoomId),
    CONSTRAINT FK_RCR_ReqRoom      FOREIGN KEY (RequestedRoomId)  REFERENCES Rooms(RoomId),
    CONSTRAINT FK_RCR_CurrentShift FOREIGN KEY (CurrentShiftId)   REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_RCR_ReqShift     FOREIGN KEY (RequestedShiftId) REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_RCR_ReviewedBy   FOREIGN KEY (ReviewedBy)       REFERENCES Users(UserId)
);
GO

/* =====================================================================
   8. SYSTEM SETTINGS
   ===================================================================== */

CREATE TABLE SystemSettings (
    SettingKey      NVARCHAR(50) PRIMARY KEY,
    SettingValue    NVARCHAR(255) NOT NULL,
    Description     NVARCHAR(255) NULL
);
GO

/* =====================================================================
   9. TRIGGERS - BUSINESS RULES ENFORCEMENT
   ===================================================================== */

-- ── 9.1 BR-06: Facilities.ManagerId phải là FacilityManager (Error: 50014) ──
CREATE OR ALTER TRIGGER trg_Facilities_Validate
ON Facilities
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.ManagerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE i.ManagerId IS NOT NULL
          AND (r.RoleName <> 'FacilityManager' OR u.IsDeleted = 1)
    )
    BEGIN
        THROW 50014, N'INVALID_ROLE: Người phụ trách cơ sở phải có vai trò FacilityManager và chưa bị xóa.', 1;
    END
END;
GO

-- ── 9.2 BR-04 & BR-11: Đóng phòng bảo trì (Condition = Closed) (Error: 50011) ──
CREATE OR ALTER TRIGGER trg_Rooms_ClosedHasFutureSession
ON Rooms
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted WHERE Condition = 'Closed')
        RETURN;

    -- Không cho đóng phòng khi còn buổi Scheduled từ hôm nay trở đi
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions cs ON cs.RoomId = i.RoomId
                             AND cs.Status = 'Scheduled'
                             AND cs.SessionDate >= CAST(GETDATE() AS DATE)
    )
    BEGIN
        THROW 50011, N'ROOM_HAS_SCHEDULE: Phòng vẫn còn buổi học sắp tới (Scheduled). Hãy dời hoặc hủy các buổi đó trước khi đóng phòng.', 1;
    END
END;
GO

-- ── 9.3 BR-04, BR-06, BR-12: Kiểm tra Classes (Error: 50010, 50014) ──
CREATE OR ALTER TRIGGER trg_Classes_Validate
ON Classes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- BR-06: LecturerId phải có Role là Lecturer và chưa bị xóa
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.LecturerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName <> 'Lecturer' OR u.IsDeleted = 1
    )
    BEGIN
        THROW 50014, N'INVALID_ROLE: Giảng viên của lớp phải có vai trò Lecturer và chưa bị xóa.', 1;
    END

    -- BR-04: Phòng mặc định không được là Closed khi lớp ở trạng thái Active
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Rooms rm ON rm.RoomId = i.RoomId
        WHERE i.Status = 'Active' AND rm.Condition = 'Closed'
    )
    BEGIN
        THROW 50010, N'ROOM_CLOSED: Phòng mặc định đang đóng bảo trì (Closed). Không thể gán cho lớp Active.', 1;
    END

    -- BR-12: StartDate <= EndDate
    IF EXISTS (
        SELECT 1 FROM inserted WHERE StartDate > EndDate
    )
    BEGIN
        THROW 50012, N'INVALID_DATE: StartDate phải nhỏ hơn hoặc bằng EndDate.', 1;
    END
END;
GO

-- ── 9.4 BR-04, BR-06, BR-03(b): Kiểm tra ClassSessions (Error: 50010, 50014, 50012) ──
CREATE OR ALTER TRIGGER trg_ClassSessions_Validate
ON ClassSessions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- BR-06: LecturerId của buổi học phải là Lecturer
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.LecturerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName <> 'Lecturer' OR u.IsDeleted = 1
    )
    BEGIN
        THROW 50014, N'INVALID_ROLE: Giảng viên phụ trách buổi học phải có vai trò Lecturer.', 1;
    END

    -- BR-04: Không xếp buổi Scheduled vào phòng Closed
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Rooms rm ON rm.RoomId = i.RoomId
        WHERE i.Status = 'Scheduled' AND rm.Condition = 'Closed'
    )
    BEGIN
        THROW 50010, N'ROOM_CLOSED: Phòng đang đóng bảo trì (Closed), không thể xếp lịch buổi học.', 1;
    END

    -- BR-03(b): Chống buổi học mới/đổi lịch làm trùng lịch của các học viên đang ghi danh Active
    -- Kiểm tra nếu có học viên của lớp này đang có một buổi học khác (Scheduled) cùng ngày, cùng ca
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Enrollments e ON e.ClassId = i.ClassId AND e.Status = 'Active'
        JOIN Enrollments e2 ON e2.StudentId = e.StudentId AND e2.ClassId <> i.ClassId AND e2.Status = 'Active'
        JOIN ClassSessions cs2 ON cs2.ClassId = e2.ClassId
                               AND cs2.SessionDate = i.SessionDate
                               AND cs2.ShiftId = i.ShiftId
                               AND cs2.SessionId <> i.SessionId
                               AND cs2.Status <> 'Cancelled'
        WHERE i.Status <> 'Cancelled'
    )
    BEGIN
        THROW 50012, N'CONFLICT_STUDENT_SESSION: Buổi học bị trùng ca và ngày với buổi học của lớp khác mà học viên đã ghi danh.', 1;
    END
END;
GO

-- ── 9.5 BR-03(a), BR-06: Kiểm tra Enrollments (Error: 50014, 50013) ──
CREATE OR ALTER TRIGGER trg_Enrollments_Validate
ON Enrollments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- BR-06: StudentId phải là Student
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.StudentId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName <> 'Student' OR u.IsDeleted = 1
    )
    BEGIN
        THROW 50014, N'INVALID_ROLE: Tài khoản ghi danh phải có vai trò Student.', 1;
    END

    -- BR-03(a): Học viên không học 2 lớp trùng lịch (kiểm tra theo từng buổi học thực tế)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions cs1 ON cs1.ClassId = i.ClassId
                              AND cs1.Status <> 'Cancelled'
                              AND cs1.SessionDate >= i.EnrollDate
        JOIN Enrollments e2    ON e2.StudentId = i.StudentId AND e2.ClassId <> i.ClassId AND e2.Status = 'Active'
        JOIN ClassSessions cs2 ON cs2.ClassId = e2.ClassId
                               AND cs2.SessionDate = cs1.SessionDate
                               AND cs2.ShiftId = cs1.ShiftId
                               AND cs2.Status <> 'Cancelled'
                               AND cs2.SessionDate >= e2.EnrollDate
        WHERE i.Status = 'Active'
    )
    BEGIN
        THROW 50013, N'CONFLICT_STUDENT_ENROLL: Học viên đã có lớp học khác cùng ca và ngày trong khoảng thời gian này.', 1;
    END
END;
GO

-- ── 9.6 BR-07: Điểm danh (Error: 50015, 50016, 50017) ──
CREATE OR ALTER TRIGGER trg_Attendance_Validate
ON Attendance
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 50015: Học viên phải đã ghi danh lớp của buổi học đó (Active tại ngày diễn ra buổi học)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions cs ON cs.SessionId = i.SessionId
        LEFT JOIN Enrollments e ON e.ClassId = cs.ClassId
                               AND e.StudentId = i.StudentId
                               AND e.EnrollDate <= cs.SessionDate
        WHERE e.EnrollmentId IS NULL
    )
    BEGIN
        THROW 50015, N'ATTENDANCE_NOT_ENROLLED: Học viên chưa ghi danh vào lớp của buổi học này.', 1;
    END

    -- 50016: Chỉ giảng viên của buổi học đó mới được điểm danh hoặc sửa điểm danh
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions cs ON cs.SessionId = i.SessionId
        WHERE (i.RecordedBy <> cs.LecturerId)
           OR (i.UpdatedBy IS NOT NULL AND i.UpdatedBy <> cs.LecturerId)
    )
    BEGIN
        THROW 50016, N'ATTENDANCE_FORBIDDEN: Chỉ giảng viên được phân công phụ trách buổi học mới có quyền điểm danh.', 1;
    END

    -- 50017: Không điểm danh buổi đã Cancelled hoặc buổi chưa diễn ra (SessionDate > hôm nay)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions cs ON cs.SessionId = i.SessionId
        WHERE cs.Status = 'Cancelled'
           OR cs.SessionDate > CAST(GETDATE() AS DATE)
    )
    BEGIN
        THROW 50017, N'ATTENDANCE_SESSION_INVALID: Không thể điểm danh buổi học đã hủy hoặc buổi học chưa diễn ra.', 1;
    END
END;
GO

/* =====================================================================
   10. INDEXES TỐI ƯU TRUY VẤN
   ===================================================================== */
CREATE INDEX IX_Users_RoleId           ON Users(RoleId);
CREATE INDEX IX_Classes_LecturerId     ON Classes(LecturerId);
CREATE INDEX IX_Classes_RoomShift      ON Classes(RoomId, ShiftId);
CREATE INDEX IX_Sessions_ClassDate     ON ClassSessions(ClassId, SessionDate);
CREATE INDEX IX_Attendance_StudentId   ON Attendance(StudentId);
CREATE INDEX IX_Enrollments_ClassId    ON Enrollments(ClassId);
CREATE INDEX IX_Tuitions_StudentStatus ON Tuitions(StudentId, PaymentStatus);
CREATE INDEX IX_RCR_Status             ON RoomChangeRequests(Status);
GO

/* =====================================================================
   11. VIEWS & FUNCTIONS
   ===================================================================== */

-- ── 11.1 View Dashboard Admin (F10) ──
CREATE OR ALTER VIEW dbo.vw_AdminStats
AS
SELECT
    (SELECT COUNT(*) FROM Rooms)                                                AS TotalRooms,
    (SELECT COUNT(*) FROM Rooms WHERE Condition = 'Open')                      AS OpenRooms,
    (SELECT COUNT(*) FROM Rooms WHERE Condition = 'Closed')                    AS ClosedRooms,
    (SELECT COUNT(*) FROM Classes WHERE Status = 'Active')                     AS ActiveClasses,
    (SELECT COUNT(DISTINCT u.UserId) FROM Users u JOIN Roles r ON r.RoleId = u.RoleId WHERE r.RoleName = 'Lecturer' AND u.IsDeleted = 0 AND u.IsActive = 1) AS ActiveLecturers,
    (SELECT COUNT(DISTINCT u.UserId) FROM Users u JOIN Roles r ON r.RoleId = u.RoleId WHERE r.RoleName = 'Student'  AND u.IsDeleted = 0 AND u.IsActive = 1) AS ActiveStudents,
    (SELECT COUNT(*) FROM RoomChangeRequests WHERE Status = 'Pending')          AS PendingChangeRequests,
    ISNULL((
        SELECT SUM(ISNULL(t.TotalAmount, 0) - ISNULL((
            SELECT SUM(p.AmountPaid)
            FROM Payments p
            WHERE p.TuitionId = t.TuitionId
        ), 0))
        FROM Tuitions t
        WHERE t.PaymentStatus <> 'Paid'
    ), 0) AS TotalUnpaidTuition;
GO

-- ── 11.2 Function Lưới trạng thái Phòng x Ca (F6) ──
CREATE OR ALTER FUNCTION dbo.fn_RoomShiftStatus (@TargetDate DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT
        f.FacilityId,
        f.FacilityName,
        r.RoomId,
        r.RoomNumber,
        r.Capacity,
        r.Condition AS RoomCondition,
        s.ShiftId,
        s.ShiftName,
        s.StartTime,
        s.EndTime,
        CASE
            WHEN r.Condition = 'Closed' THEN N'Closed for Maintenance'
            WHEN cs.SessionId IS NOT NULL THEN N'Occupied'
            ELSE N'Empty'
        END AS SlotStatus,
        cs.ClassId,
        c.ClassName,
        cs.LecturerId,
        u.FullName AS LecturerName,
        cs.SessionId
    FROM Facilities f
    JOIN Rooms r ON r.FacilityId = f.FacilityId
    CROSS JOIN Shifts s
    LEFT JOIN ClassSessions cs ON cs.RoomId = r.RoomId
                              AND cs.ShiftId = s.ShiftId
                              AND cs.SessionDate = @TargetDate
                              AND cs.Status <> 'Cancelled'
    LEFT JOIN Classes c ON c.ClassId = cs.ClassId
    LEFT JOIN Users u ON u.UserId = cs.LecturerId
);
GO

-- ── 11.3 Function Thống kê Điểm danh & Học phí tạm tính (F5) ──
CREATE OR ALTER FUNCTION dbo.fn_StudentAttendanceTuition (
    @PeriodFrom DATE,
    @PeriodTo   DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        e.StudentId,
        u.FullName AS StudentName,
        e.ClassId,
        c.ClassName,
        ISNULL(SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END), 0) AS PresentCount,
        ISNULL(SUM(CASE WHEN a.Status = 'ExcusedAbsent' THEN 1 ELSE 0 END), 0) AS ExcusedCount,
        ISNULL(SUM(CASE WHEN a.Status = 'UnexcusedAbsent' THEN 1 ELSE 0 END), 0) AS UnexcusedCount,
        SUM(CASE WHEN a.Status IN ('Present','UnexcusedAbsent') THEN 1 ELSE 0 END) AS BillableSessions,
        COUNT(a.AttendanceId) AS TotalSessions
    FROM ClassSessions cs JOIN Classes c ON c.ClassId = cs.ClassId
    JOIN Attendance a ON a.SessionId = cs.SessionId JOIN Users u ON u.UserId = a.StudentId
    WHERE cs.SessionDate BETWEEN @PeriodFrom AND @PeriodTo AND cs.Status = 'Completed'
    GROUP BY a.StudentId, u.FullName, cs.ClassId, c.ClassName
);
GO

/* =====================================================================
   12. SEED DATA KHỞI TẠO
   ===================================================================== */

-- 12.1 Roles
INSERT INTO Roles (RoleName) VALUES
('Admin'), ('GrandManager'), ('FacilityManager'), ('Lecturer'), ('Student');
GO

-- 12.2 Facilities (20 cơ sở theo yêu cầu seed data; 3 cơ sở đầu giữ nguyên)
INSERT INTO Facilities (FacilityName, Address) VALUES
(N'Cơ sở 1', N'123 Nguyễn Huệ, Quận 1, TP.HCM'),
(N'Cơ sở 2', N'456 Lê Duẩn, Quận 1, TP.HCM'),
(N'Cơ sở 3', N'789 Võ Văn Ngân, TP.Thủ Đức, TP.HCM');

DECLARE @FacilityNumber INT = 4;
WHILE @FacilityNumber <= 20
BEGIN
    INSERT INTO Facilities (FacilityName, Address)
    VALUES (CONCAT(N'Cơ sở ', @FacilityNumber), CONCAT(N'Địa chỉ mẫu cơ sở ', @FacilityNumber, N', TP.HCM'));
    SET @FacilityNumber += 1;
END
GO

-- 12.3 Rooms (20 cơ sở x 2 phòng = 40 phòng)
DECLARE @FacilityId INT = 1;
WHILE @FacilityId <= 20
BEGIN
    DECLARE @RoomNumber INT = 1;
    WHILE @RoomNumber <= 2
    BEGIN
        INSERT INTO Rooms (FacilityId, RoomNumber, Capacity, Condition)
        VALUES
        (
            @FacilityId,
            CONCAT('P', RIGHT(CONCAT('00', @FacilityId), 2), RIGHT(CONCAT('00', @RoomNumber), 2)),
            30,
            'Open'
        );
        SET @RoomNumber += 1;
    END
    SET @FacilityId += 1;
END
GO

-- 12.4 Shifts (3 ca chiều/tối, mỗi ca 90 phút)
INSERT INTO Shifts (ShiftName, StartTime, EndTime) VALUES
(N'Ca 1', '16:30:00', '18:00:00'),
(N'Ca 2', '18:00:00', '19:30:00'),
(N'Ca 3', '19:30:00', '21:00:00');
GO

-- 12.5 Subjects (5 môn học)
INSERT INTO Subjects (SubjectName) VALUES
('Math'), ('Literature'), ('Physics'), ('English'), ('Chemistry');
GO

-- 12.6 System Settings
INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('TUITION_PER_SESSION', '200000', N'Học phí mặc định mỗi buổi học (VNĐ)');
GO

-- 12.7 Seed tài khoản và dữ liệu nghiệp vụ.
-- Giữ nguyên số lượng danh mục Roles (5), Shifts (3), Subjects (5) và SystemSettings (1).
-- Mật khẩu mẫu của các tài khoản: Dev@12345.
DECLARE @DemoPasswordHash NVARCHAR(255) = N'$2a$11$9a.BKnjMG.j5tmrIiZdSc..d0z/Yg8WPLN9vd1SQRkjL9ld.URIey';
DECLARE @AdminRoleId INT;
DECLARE @GrandManagerRoleId INT;
DECLARE @FacilityManagerRoleId INT;
DECLARE @LecturerRoleId INT;
DECLARE @StudentRoleId INT;
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @i INT;

SELECT
    @AdminRoleId = MAX(CASE WHEN RoleName = N'Admin' THEN RoleId END),
    @GrandManagerRoleId = MAX(CASE WHEN RoleName = N'GrandManager' THEN RoleId END),
    @FacilityManagerRoleId = MAX(CASE WHEN RoleName = N'FacilityManager' THEN RoleId END),
    @LecturerRoleId = MAX(CASE WHEN RoleName = N'Lecturer' THEN RoleId END),
    @StudentRoleId = MAX(CASE WHEN RoleName = N'Student' THEN RoleId END)
FROM Roles;

INSERT INTO Users (FullName, Email, PasswordHash, RoleId, IsActive, IsDeleted)
VALUES (N'System Admin', N'admin@tutorflow.local', @DemoPasswordHash, @AdminRoleId, 1, 0);

INSERT INTO Users (FullName, Email, PasswordHash, RoleId, IsActive, IsDeleted)
VALUES (N'Grand Manager', N'gm@tutorflow.local', @DemoPasswordHash, @GrandManagerRoleId, 1, 0);

SET @i = 1;
WHILE @i <= 20
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
    VALUES
    (
        CONCAT(N'Facility Manager ', @i),
        CONCAT(N'fm', @i, N'@tutorflow.local'),
        @DemoPasswordHash,
        CONCAT(N'090200', RIGHT(CONCAT(N'00', @i), 2)),
        @FacilityManagerRoleId,
        1,
        0
    );
    SET @i += 1;
END

-- Thêm 14 học viên riêng cho từng lớp mẫu để mỗi lớp có tổng cộng 15 học viên.
DECLARE @ClassNumber INT = 1;
DECLARE @StudentSlot INT;
WHILE @ClassNumber <= 20
BEGIN
    SET @StudentSlot = 2;
    WHILE @StudentSlot <= 15
    BEGIN
        INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
        VALUES
        (
            CONCAT(N'Học viên lớp ', RIGHT(CONCAT(N'00', @ClassNumber), 2), N' - ', RIGHT(CONCAT(N'00', @StudentSlot), 2)),
            CONCAT(N'student_demo_', RIGHT(CONCAT(N'00', @ClassNumber), 2), N'_', RIGHT(CONCAT(N'00', @StudentSlot), 2), N'@tutorflow.local'),
            @DemoPasswordHash,
            CONCAT(N'0985', RIGHT(CONCAT(N'00', @ClassNumber), 2), RIGHT(CONCAT(N'00', @StudentSlot), 2)),
            @StudentRoleId,
            1,
            0
        );
        SET @StudentSlot += 1;
    END
    SET @ClassNumber += 1;
END

SET @i = 1;
WHILE @i <= 10
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
    VALUES
    (
        CONCAT(N'Giảng viên mẫu ', @i),
        CONCAT(N'lecturer', @i, N'@tutorflow.local'),
        @DemoPasswordHash,
        CONCAT(N'091300', RIGHT(CONCAT(N'00', @i), 2)),
        @LecturerRoleId,
        1,
        0
    );
    SET @i += 1;
END

SET @i = 1;
WHILE @i <= 20
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsDeleted)
    VALUES
    (
        CONCAT(N'Học viên mẫu ', @i),
        CONCAT(N'student', @i, N'@tutorflow.local'),
        @DemoPasswordHash,
        CONCAT(N'098400', RIGHT(CONCAT(N'00', @i), 2)),
        @StudentRoleId,
        1,
        0
    );
    SET @i += 1;
END

-- Gán mỗi Facility Manager một cơ sở (mỗi người phụ trách tối đa một cơ sở).
UPDATE f
SET ManagerId = fm.UserId
FROM Facilities AS f
JOIN Users AS fm
    ON fm.Email = CONCAT(N'fm', f.FacilityId, N'@tutorflow.local');

DECLARE @DemoRows TABLE (RowNumber INT NOT NULL PRIMARY KEY);
INSERT INTO @DemoRows (RowNumber) VALUES
(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
(11),(12),(13),(14),(15),(16),(17),(18),(19),(20);

-- 12.8 Classes: 20 lớp trải trên 20 cơ sở.
;WITH RankedSubjects AS
(
    SELECT SubjectId, ROW_NUMBER() OVER (ORDER BY SubjectId) AS RowNumber
    FROM Subjects
),
RankedShifts AS
(
    SELECT ShiftId, ROW_NUMBER() OVER (ORDER BY ShiftId) AS RowNumber
    FROM Shifts
)
INSERT INTO Classes
    (ClassName, SubjectId, LecturerId, FacilityId, RoomId, ShiftId, DaysOfWeek, StartDate, EndDate, Status, CreatedBy)
SELECT
    CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2)),
    sb.SubjectId,
    lecturer.UserId,
    f.FacilityId,
    rm.RoomId,
    sh.ShiftId,
    N'2,3,4,5,6,7,8',
    DATEADD(DAY, -30, @Today),
    DATEADD(DAY, 180, @Today),
    N'Active',
    gm.UserId
FROM @DemoRows AS demo
JOIN Facilities AS f
    ON f.FacilityName = CONCAT(N'Cơ sở ', demo.RowNumber)
JOIN Rooms AS rm
    ON rm.FacilityId = f.FacilityId
   AND rm.RoomNumber = CONCAT('P', RIGHT(CONCAT('00', demo.RowNumber), 2), '01')
JOIN Users AS lecturer
    ON lecturer.Email = CONCAT(N'lecturer', ((demo.RowNumber - 1) % 10) + 1, N'@tutorflow.local')
JOIN Users AS gm ON gm.Email = N'gm@tutorflow.local'
JOIN RankedSubjects AS sb
    ON sb.RowNumber = ((demo.RowNumber - 1) % (SELECT COUNT(*) FROM Subjects)) + 1
JOIN RankedShifts AS sh
    ON sh.RowNumber = ((demo.RowNumber - 1) % (SELECT COUNT(*) FROM Shifts)) + 1;

-- 12.9 ClassSessions: một buổi đã hoàn thành cho mỗi lớp, trên các ngày riêng biệt.
INSERT INTO ClassSessions (ClassId, SessionDate, RoomId, ShiftId, LecturerId, Status)
SELECT
    c.ClassId,
    DATEADD(DAY, demo.RowNumber - 20, @Today),
    c.RoomId,
    c.ShiftId,
    c.LecturerId,
    N'Completed'
FROM @DemoRows AS demo
JOIN Classes AS c
    ON c.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2));

-- 12.10 Enrollments: mỗi lớp mẫu có 15 học viên riêng.
INSERT INTO Enrollments (StudentId, ClassId, EnrollDate, Status)
SELECT
    st.UserId,
    c.ClassId,
    DATEADD(DAY, -30, @Today),
    N'Active'
FROM @DemoRows AS demo
JOIN (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15)) AS slot(SlotNumber)
    ON 1 = 1
JOIN Users AS st
    ON st.Email = CASE
        WHEN slot.SlotNumber = 1
            THEN CONCAT(N'student', demo.RowNumber, N'@tutorflow.local')
        ELSE CONCAT(N'student_demo_', RIGHT(CONCAT(N'00', demo.RowNumber), 2), N'_', RIGHT(CONCAT(N'00', slot.SlotNumber), 2), N'@tutorflow.local')
    END
JOIN Classes AS c
    ON c.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2));

-- 12.11 Attendance: mỗi học viên được điểm danh tại buổi của lớp.
INSERT INTO Attendance (SessionId, StudentId, Status, Note, RecordedBy)
SELECT
    cs.SessionId,
    enrollment.StudentId,
    N'Present',
    N'Dữ liệu điểm danh mẫu',
    cs.LecturerId
FROM Enrollments AS enrollment
JOIN ClassSessions AS cs ON cs.ClassId = enrollment.ClassId;

-- 12.12 Tuitions: invoices for the previous, completed calendar month only.
DECLARE @TuitionPeriodFrom DATE = DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1));
DECLARE @TuitionPeriodTo DATE = DATEADD(DAY, -1, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1));
INSERT INTO Tuitions
    (StudentId, ClassId, PeriodFrom, PeriodTo, BillableSessions, AmountPerSession, PaymentStatus, DueDate)
SELECT
    enrollment.StudentId,
    enrollment.ClassId,
    @TuitionPeriodFrom,
    @TuitionPeriodTo,
    attendance.BillableSessions,
    200000,
    CASE TRY_CONVERT(INT, RIGHT(c.ClassName, 2)) % 3 WHEN 0 THEN N'Paid' WHEN 1 THEN N'PartiallyPaid' ELSE N'Unpaid' END,
    DATEADD(DAY, 7, @Today)
FROM Enrollments AS enrollment
JOIN Classes AS c ON c.ClassId = enrollment.ClassId
CROSS APPLY
(
    SELECT COUNT(*) AS BillableSessions
    FROM Attendance AS a
    JOIN ClassSessions AS cs ON cs.SessionId = a.SessionId
    WHERE a.StudentId = enrollment.StudentId
      AND cs.ClassId = enrollment.ClassId
      AND cs.Status = N'Completed'
      AND cs.SessionDate BETWEEN @TuitionPeriodFrom AND @TuitionPeriodTo
      AND a.Status IN (N'Present', N'UnexcusedAbsent')
) AS attendance
WHERE attendance.BillableSessions > 0;

-- 12.13 Payments: partial/full receipts for already-issued sample invoices.
INSERT INTO Payments (TuitionId, AmountPaid, PaymentDate, PaymentMethod, ReceivedBy, ReceiptNumber)
SELECT
    tuition.TuitionId,
    CASE WHEN tuition.PaymentStatus = N'Paid' THEN tuition.TotalAmount ELSE tuition.TotalAmount / 2 END,
    GETDATE(),
    CASE WHEN tuition.TuitionId % 2 = 0 THEN N'Chuyển khoản' ELSE N'Tiền mặt' END,
    gm.UserId,
    CONCAT(N'TF-DEMO-', RIGHT(CONCAT(N'0000', tuition.TuitionId), 4))
FROM Tuitions AS tuition
CROSS JOIN Users AS gm
WHERE gm.Email = N'gm@tutorflow.local'
  AND tuition.PeriodFrom = @TuitionPeriodFrom
  AND tuition.PeriodTo = @TuitionPeriodTo
  AND tuition.PaymentStatus IN (N'Paid', N'PartiallyPaid');
-- 12.14 RoomChangeRequests: 20 yêu cầu với đủ trạng thái chờ duyệt/đã duyệt/từ chối.
INSERT INTO RoomChangeRequests
    (LecturerId, ClassId, SessionId, CurrentRoomId, RequestedRoomId, CurrentShiftId,
     RequestedShiftId, Reason, Status, ReviewedBy, ReviewedAt, ReviewNote)
SELECT
    c.LecturerId,
    c.ClassId,
    cs.SessionId,
    c.RoomId,
    requestedRoom.RoomId,
    c.ShiftId,
    ((c.ShiftId % (SELECT COUNT(*) FROM Shifts)) + 1),
    N'Yêu cầu đổi phòng/ca mẫu',
    CASE TRY_CONVERT(INT, RIGHT(c.ClassName, 2)) % 3
        WHEN 0 THEN N'Approved'
        WHEN 1 THEN N'Pending'
        ELSE N'Rejected'
    END,
    CASE WHEN TRY_CONVERT(INT, RIGHT(c.ClassName, 2)) % 3 = 1 THEN NULL ELSE gm.UserId END,
    CASE WHEN TRY_CONVERT(INT, RIGHT(c.ClassName, 2)) % 3 = 1 THEN NULL ELSE GETDATE() END,
    CASE WHEN TRY_CONVERT(INT, RIGHT(c.ClassName, 2)) % 3 = 1 THEN NULL ELSE N'Đã xử lý yêu cầu mẫu' END
FROM @DemoRows AS demo
JOIN Classes AS c
    ON c.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
JOIN ClassSessions AS cs ON cs.ClassId = c.ClassId
JOIN Rooms AS requestedRoom
    ON requestedRoom.FacilityId = c.FacilityId
   AND requestedRoom.RoomNumber = CONCAT('P', RIGHT(CONCAT('00', demo.RowNumber), 2), '02')
CROSS JOIN Users AS gm
WHERE gm.Email = N'gm@tutorflow.local';

-- Kiểm tra dữ liệu mẫu theo yêu cầu: tối thiểu 20 bản ghi mỗi bảng nghiệp vụ.
IF (SELECT COUNT_BIG(*) FROM Users) < 20
    OR (SELECT COUNT_BIG(*) FROM Facilities) < 20
    OR (SELECT COUNT_BIG(*) FROM Rooms) < 20
    OR (SELECT COUNT_BIG(*) FROM Classes) < 20
    OR (SELECT COUNT_BIG(*) FROM ClassSessions) < 20
    OR (SELECT COUNT_BIG(*) FROM Enrollments) < 20
    OR (SELECT COUNT_BIG(*) FROM Attendance) < 20
    OR (SELECT COUNT_BIG(*) FROM Tuitions) < 20
    OR (SELECT COUNT_BIG(*) FROM Payments) < 20
    OR (SELECT COUNT_BIG(*) FROM RoomChangeRequests) < 20
BEGIN
    THROW 51000, N'Dữ liệu mẫu chưa đạt tối thiểu 20 bản ghi cho mỗi bảng nghiệp vụ.', 1;
END

IF EXISTS
(
    SELECT 1
    FROM @DemoRows AS demo
    JOIN Classes AS sampleClass
      ON sampleClass.ClassName = CONCAT(N'Lớp mẫu ', RIGHT(CONCAT(N'00', demo.RowNumber), 2))
    WHERE
    (
        SELECT COUNT_BIG(*)
        FROM Enrollments enrollment
        WHERE enrollment.ClassId = sampleClass.ClassId
          AND enrollment.Status = N'Active'
    ) < 15
)
BEGIN
    THROW 51004, N'Mỗi lớp mẫu phải có ít nhất 15 học viên đang ghi danh.', 1;
END

SELECT N'Users' AS TableName, COUNT_BIG(*) AS RecordCount FROM Users
UNION ALL SELECT N'Facilities', COUNT_BIG(*) FROM Facilities
UNION ALL SELECT N'Rooms', COUNT_BIG(*) FROM Rooms
UNION ALL SELECT N'Classes', COUNT_BIG(*) FROM Classes
UNION ALL SELECT N'ClassSessions', COUNT_BIG(*) FROM ClassSessions
UNION ALL SELECT N'Enrollments', COUNT_BIG(*) FROM Enrollments
UNION ALL SELECT N'Attendance', COUNT_BIG(*) FROM Attendance
UNION ALL SELECT N'Tuitions', COUNT_BIG(*) FROM Tuitions
UNION ALL SELECT N'Payments', COUNT_BIG(*) FROM Payments
UNION ALL SELECT N'RoomChangeRequests', COUNT_BIG(*) FROM RoomChangeRequests;

SELECT sampleClass.ClassName, COUNT_BIG(enrollment.EnrollmentId) AS ActiveStudentCount
FROM Classes AS sampleClass
LEFT JOIN Enrollments AS enrollment
    ON enrollment.ClassId = sampleClass.ClassId
   AND enrollment.Status = N'Active'
WHERE sampleClass.ClassName LIKE N'Lớp mẫu [0-9][0-9]'
GROUP BY sampleClass.ClassName
ORDER BY sampleClass.ClassName;
GO
