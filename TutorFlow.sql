/* =====================================================================
   PROJECT   : TutorFlow  (đổi tên từ EduCenterManagement)
   VERSION   : 2.0
   PLATFORM  : SQL Server 2016+ (tương thích .NET 8 / EF Core 8)
   MÔ TẢ     : Database cho 5 vai trò - Admin, Grand Manager,
               Facility Manager, Lecturer, Student

   CHANGELOG v2.0 (so với v1):
   ---- Chống trùng lịch (4 nhận xét chính) ----
   ① Phòng trùng ca/ngày  : giữ UNIQUE FILTERED INDEX
                            UX_Sessions_Room_Shift_Date (RoomId, ShiftId, SessionDate)
   ② Giảng viên trùng lịch: THAY trigger bằng UNIQUE FILTERED INDEX
                            UX_Sessions_Lecturer_Shift_Date (LecturerId, ShiftId, SessionDate)
                            -> an toàn khi nhiều giao dịch chạy song song.
                            ClassSessions có thêm cột LecturerId (hỗ trợ dạy thay).
   ③ Học viên trùng lịch  : kiểm tra theo TỪNG BUỔI HỌC thật (SessionDate + ShiftId)
                            ở 2 nơi: khi ghi danh (Enrollments) và khi tạo/đổi buổi
                            (ClassSessions). Bắt được cả trường hợp đổi ca 1 buổi.
   ④ Phòng Closed         : chặn ở ClassSessions, Classes (phòng mặc định) và chặn
                            đóng phòng khi còn buổi sắp tới.
   ---- Lỗ hổng phát hiện thêm ----
   ⑤ Classes.FacilityId phải khớp Rooms.FacilityId (composite FK)
   ⑥ Kiểm tra đúng vai trò: LecturerId phải là Lecturer, StudentId phải là Student,
      ManagerId phải là FacilityManager
   ⑦ Facilities.ManagerId: gắn Facility Manager với cơ sở (phân quyền theo cơ sở)
   ⑧ Attendance: học viên phải có ghi danh, người điểm danh phải là giảng viên của
      buổi đó, không điểm danh buổi đã hủy / buổi chưa diễn ra
   ⑨ Classes.EndDate NOT NULL (cần để sinh buổi học), thêm CHECK định dạng DaysOfWeek
   ⑩ Trigger dùng THROW với mã lỗi riêng 50010..50017 (C# map lỗi dễ dàng)
   ⑪ Soft delete: Users.IsDeleted
   ⑫ Thêm View/Function phục vụ màn hình: fn_RoomShiftStatus, vw_AdminStats,
      fn_StudentAttendanceTuition

   QUY ƯỚC DaysOfWeek: 2=Thứ 2 ... 7=Thứ 7, 8=Chủ nhật  (vd "2,4,6")

   BẢNG MÃ LỖI (SqlException.Number)
     2601 / 2627 + tên index UX_Sessions_Room_Shift_Date     -> ROOM_CONFLICT
     2601 / 2627 + tên index UX_Sessions_Lecturer_Shift_Date -> LECTURER_CONFLICT
     2601 / 2627 + tên constraint UQ_Session                 -> SESSION_DUPLICATE
     50010 ROOM_CLOSED               phòng đang đóng, không xếp lịch được
     50011 ROOM_HAS_SCHEDULE         đóng phòng nhưng còn buổi sắp tới
     50012 CONFLICT_STUDENT_SESSION  buổi học làm trùng lịch của học viên đã ghi danh
     50013 CONFLICT_STUDENT_ENROLL   ghi danh làm trùng lịch học viên
     50014 INVALID_ROLE              sai vai trò (giảng viên / học viên / quản lý cơ sở)
     50015 ATTENDANCE_NOT_ENROLLED   học viên chưa ghi danh lớp của buổi học
     50016 ATTENDANCE_FORBIDDEN      người điểm danh không phải giảng viên của buổi
     50017 ATTENDANCE_SESSION_INVALID buổi đã hủy hoặc chưa diễn ra
     1205  Deadlock -> ứng dụng nên retry giao dịch
   ===================================================================== */

CREATE DATABASE TutorFlow;
GO
USE TutorFlow;
GO

-- Bắt buộc bật để dùng filtered index / computed column (sqlcmd mặc định đang tắt)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
    RoleId          INT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,           -- khóa / kích hoạt tài khoản
    IsDeleted       BIT NOT NULL DEFAULT 0,           -- xóa mềm (Delete trên giao diện = IsDeleted = 1)
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

/* =====================================================================
   2. FACILITIES & ROOMS  (3 cơ sở x 6 phòng)
   ===================================================================== */

CREATE TABLE Facilities (
    FacilityId      INT IDENTITY(1,1) PRIMARY KEY,
    FacilityName    NVARCHAR(100) NOT NULL,
    Address         NVARCHAR(255) NULL,
    ManagerId       INT NULL,                          -- Facility Manager phụ trách cơ sở
    CONSTRAINT FK_Facilities_Manager FOREIGN KEY (ManagerId) REFERENCES Users(UserId)
);
GO

-- Mỗi Facility Manager chỉ phụ trách tối đa 1 cơ sở
CREATE UNIQUE INDEX UX_Facilities_Manager
    ON Facilities (ManagerId)
    WHERE ManagerId IS NOT NULL;
GO

CREATE TABLE Rooms (
    RoomId          INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId      INT NOT NULL,
    RoomNumber      NVARCHAR(20) NOT NULL,             -- vd: P101
    Capacity        INT NULL CHECK (Capacity IS NULL OR Capacity > 0),
    Condition       NVARCHAR(20) NOT NULL DEFAULT 'Open'
                        CHECK (Condition IN ('Open','Closed')),  -- Open / Closed for Maintenance
    CONSTRAINT FK_Rooms_Facilities FOREIGN KEY (FacilityId) REFERENCES Facilities(FacilityId),
    CONSTRAINT UQ_Room_Facility UNIQUE (FacilityId, RoomNumber),
    -- Cần để Classes tham chiếu composite FK (RoomId, FacilityId)
    CONSTRAINT UQ_Room_Id_Facility UNIQUE (RoomId, FacilityId)
);
GO

/* =====================================================================
   3. SHIFTS (Ca học) & SUBJECTS (Môn học)
   ===================================================================== */

CREATE TABLE Shifts (
    ShiftId         INT IDENTITY(1,1) PRIMARY KEY,
    ShiftName       NVARCHAR(50) NOT NULL UNIQUE,      -- "Ca 1", "Ca 2", "Ca 3"
    StartTime       TIME NOT NULL,
    EndTime         TIME NOT NULL,
    CONSTRAINT CK_Shifts_Time CHECK (EndTime > StartTime)
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
    LecturerId      INT NOT NULL,                       -- FK -> Users (role = Lecturer, kiểm tra bằng trigger)
    FacilityId      INT NOT NULL,
    RoomId          INT NOT NULL,                        -- phòng mặc định của lớp (phải thuộc FacilityId)
    ShiftId         INT NOT NULL,
    DaysOfWeek      NVARCHAR(20) NOT NULL,               -- vd: "2,4,6"; 2=Thứ 2 ... 7=Thứ 7, 8=Chủ nhật
    StartDate       DATE NOT NULL,
    EndDate         DATE NOT NULL,                       -- bắt buộc: dùng để sinh ClassSessions
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Active'
                        CHECK (Status IN ('Active','Closed')),
    CreatedBy       INT NULL,                            -- Grand Manager tạo
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Classes_Subjects   FOREIGN KEY (SubjectId)  REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_Classes_Lecturer   FOREIGN KEY (LecturerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Classes_Facilities FOREIGN KEY (FacilityId) REFERENCES Facilities(FacilityId),
    -- Phòng mặc định phải thuộc đúng cơ sở của lớp
    CONSTRAINT FK_Classes_Rooms      FOREIGN KEY (RoomId, FacilityId) REFERENCES Rooms(RoomId, FacilityId),
    CONSTRAINT FK_Classes_Shifts     FOREIGN KEY (ShiftId)    REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_Classes_CreatedBy  FOREIGN KEY (CreatedBy)  REFERENCES Users(UserId),
    CONSTRAINT CK_Classes_Dates      CHECK (EndDate >= StartDate),
    CONSTRAINT CK_Classes_Days       CHECK (DaysOfWeek <> '' AND DaysOfWeek NOT LIKE '%[^2-8,]%')
);
GO

-- Từng buổi học cụ thể của 1 lớp (dùng để điểm danh, xếp phòng & tính học phí)
CREATE TABLE ClassSessions (
    SessionId       INT IDENTITY(1,1) PRIMARY KEY,
    ClassId         INT NOT NULL,
    SessionDate     DATE NOT NULL,
    RoomId          INT NOT NULL,                        -- có thể khác phòng mặc định nếu đổi phòng buổi đó
    ShiftId         INT NOT NULL,                        -- có thể khác ca mặc định nếu đổi ca buổi đó
    LecturerId      INT NOT NULL,                        -- mặc định = Classes.LecturerId; khác nếu dạy thay
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Scheduled'
                        CHECK (Status IN ('Scheduled','Completed','Cancelled')),
    CONSTRAINT FK_Sessions_Classes  FOREIGN KEY (ClassId)    REFERENCES Classes(ClassId),
    CONSTRAINT FK_Sessions_Rooms    FOREIGN KEY (RoomId)     REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Sessions_Shifts   FOREIGN KEY (ShiftId)    REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_Sessions_Lecturer FOREIGN KEY (LecturerId) REFERENCES Users(UserId),
    -- Mỗi lớp chỉ có 1 buổi mỗi ngày (đổi giờ/phòng = UPDATE buổi đó, không INSERT buổi mới)
    CONSTRAINT UQ_Session UNIQUE (ClassId, SessionDate)
);
GO

/* ---- ① Chống PHÒNG bị đặt trùng ca / ngày (không tính buổi đã Cancelled) ---- */
CREATE UNIQUE INDEX UX_Sessions_Room_Shift_Date
    ON ClassSessions (RoomId, ShiftId, SessionDate)
    WHERE Status <> 'Cancelled';
GO

/* ---- ② Chống GIẢNG VIÊN dạy 2 lớp cùng ca, cùng ngày (kể cả 2 cơ sở khác nhau) ---- */
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
    EnrollDate      DATE NOT NULL DEFAULT GETDATE(),
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
    RecordedBy      INT NOT NULL,                        -- Lecturer điểm danh
    RecordedAt      DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedBy       INT NULL,                            -- Lecturer sửa điểm danh (luôn set khi UPDATE)
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
    SessionsAttended  INT NOT NULL DEFAULT 0,             -- tổng buổi "Present"
    AmountPerSession  DECIMAL(12,0) NOT NULL DEFAULT 200000,
    TotalAmount       AS (SessionsAttended * AmountPerSession) PERSISTED,
    PaymentStatus     NVARCHAR(20) NOT NULL DEFAULT 'Unpaid'
                          CHECK (PaymentStatus IN ('Paid','Unpaid')),
    DueDate           DATE NULL,
    CreatedAt         DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Tuitions_Students FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    CONSTRAINT FK_Tuitions_Classes  FOREIGN KEY (ClassId)   REFERENCES Classes(ClassId),
    CONSTRAINT CK_Tuitions_Period   CHECK (PeriodTo >= PeriodFrom),
    -- Không tạo trùng học phí cho cùng học viên / lớp / kỳ
    CONSTRAINT UQ_Tuition_Period    UNIQUE (StudentId, ClassId, PeriodFrom, PeriodTo)
);
GO

CREATE TABLE Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    TuitionId       INT NOT NULL,
    AmountPaid      DECIMAL(12,0) NOT NULL CHECK (AmountPaid > 0),
    PaymentDate     DATETIME NOT NULL DEFAULT GETDATE(),
    PaymentMethod   NVARCHAR(50) NULL,                    -- Cash, Bank Transfer, ...
    ReceivedBy      INT NULL,                             -- Grand Manager xác nhận
    ReceiptNumber   NVARCHAR(50) NULL,
    CONSTRAINT FK_Payments_Tuitions FOREIGN KEY (TuitionId)  REFERENCES Tuitions(TuitionId),
    CONSTRAINT FK_Payments_Users    FOREIGN KEY (ReceivedBy) REFERENCES Users(UserId)
);
GO

/* =====================================================================
   7. ROOM / SHIFT CHANGE REQUESTS (Lecturer -> Facility Manager)
   ===================================================================== */

CREATE TABLE RoomChangeRequests (
    RequestId         INT IDENTITY(1,1) PRIMARY KEY,
    LecturerId         INT NOT NULL,
    ClassId            INT NOT NULL,
    SessionId          INT NULL,                          -- NULL = đổi vĩnh viễn cho cả lớp
    CurrentRoomId      INT NOT NULL,
    RequestedRoomId    INT NULL,
    CurrentShiftId     INT NOT NULL,
    RequestedShiftId   INT NULL,
    Reason             NVARCHAR(500) NOT NULL,
    Status             NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                           CHECK (Status IN ('Pending','Approved','Rejected')),
    ReviewedBy         INT NULL,                          -- Facility Manager
    ReviewedAt         DATETIME NULL,
    ReviewNote         NVARCHAR(255) NULL,
    CreatedAt          DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_RCR_Lecturer     FOREIGN KEY (LecturerId)       REFERENCES Users(UserId),
    CONSTRAINT FK_RCR_Classes      FOREIGN KEY (ClassId)          REFERENCES Classes(ClassId),
    CONSTRAINT FK_RCR_Sessions     FOREIGN KEY (SessionId)        REFERENCES ClassSessions(SessionId),
    CONSTRAINT FK_RCR_CurrentRoom  FOREIGN KEY (CurrentRoomId)    REFERENCES Rooms(RoomId),
    CONSTRAINT FK_RCR_ReqRoom      FOREIGN KEY (RequestedRoomId)  REFERENCES Rooms(RoomId),
    CONSTRAINT FK_RCR_CurrentShift FOREIGN KEY (CurrentShiftId)   REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_RCR_ReqShift     FOREIGN KEY (RequestedShiftId) REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_RCR_ReviewedBy   FOREIGN KEY (ReviewedBy)       REFERENCES Users(UserId),
    -- Phải yêu cầu đổi ít nhất phòng hoặc ca
    CONSTRAINT CK_RCR_HasTarget    CHECK (RequestedRoomId IS NOT NULL OR RequestedShiftId IS NOT NULL)
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
   9. TRIGGERS (kiểm tra nghiệp vụ không diễn đạt được bằng constraint)

   Quy ước: lỗi nghiệp vụ -> ROLLBACK + THROW <mã lỗi riêng>.
   Lưu ý EF Core 7+: mọi bảng có trigger phải khai báo
   ToTable(t => t.HasTrigger("<tên trigger>")) trong DbContext.
   ===================================================================== */

/* ---------- Facilities: ManagerId phải là FacilityManager ---------- */
CREATE OR ALTER TRIGGER trg_Facilities_Validate
ON Facilities
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(ManagerId) RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.ManagerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE i.ManagerId IS NOT NULL AND r.RoleName <> 'FacilityManager'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50014, N'INVALID_ROLE: Người phụ trách cơ sở phải có vai trò FacilityManager.', 1;
    END
END;
GO

/* ---------- Rooms: không cho đóng phòng khi còn buổi học sắp tới ---------- */
CREATE OR ALTER TRIGGER trg_Rooms_ClosedHasFutureSession
ON Rooms
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(Condition) RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.RoomId = i.RoomId
        JOIN ClassSessions cs ON cs.RoomId = i.RoomId
                             AND cs.Status = 'Scheduled'
                             AND cs.SessionDate >= CAST(GETDATE() AS DATE)
        WHERE i.Condition = 'Closed' AND d.Condition <> 'Closed'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50011, N'ROOM_HAS_SCHEDULE: Phòng vẫn còn buổi học sắp tới (Scheduled). Hãy dời hoặc hủy các buổi đó trước khi đóng phòng.', 1;
    END
END;
GO

/* ---------- Classes: vai trò giảng viên, phòng Closed, đồng bộ giảng viên ---------- */
CREATE OR ALTER TRIGGER trg_Classes_Validate
ON Classes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- LecturerId phải là Lecturer
    IF UPDATE(LecturerId) AND EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.LecturerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName <> 'Lecturer'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50014, N'INVALID_ROLE: Giảng viên của lớp phải có vai trò Lecturer.', 1;
    END

    -- Phòng mặc định của lớp đang hoạt động không được là phòng Closed
    IF (UPDATE(RoomId) OR UPDATE(Status)) AND EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Rooms r ON r.RoomId = i.RoomId
        WHERE i.Status = 'Active' AND r.Condition = 'Closed'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50010, N'ROOM_CLOSED: Phòng đang đóng cửa (Closed), không thể gán cho lớp.', 1;
    END

    -- Đổi giảng viên của lớp -> đổi giảng viên cho các buổi Scheduled sắp tới
    -- (nếu trùng lịch, UX_Sessions_Lecturer_Shift_Date sẽ chặn và rollback)
    IF UPDATE(LecturerId)
    BEGIN
        UPDATE cs
        SET cs.LecturerId = i.LecturerId
        FROM ClassSessions cs
        JOIN inserted i ON i.ClassId = cs.ClassId
        JOIN deleted  d ON d.ClassId = i.ClassId
        WHERE d.LecturerId <> i.LecturerId
          AND cs.LecturerId = d.LecturerId
          AND cs.Status = 'Scheduled'
          AND cs.SessionDate >= CAST(GETDATE() AS DATE);
    END
END;
GO

/* ---------- ClassSessions: vai trò giảng viên, phòng Closed, học viên trùng lịch ---------- */
-- (Phòng trùng và giảng viên trùng đã do 2 UNIQUE FILTERED INDEX ở trên đảm nhiệm)
CREATE OR ALTER TRIGGER trg_ClassSessions_Validate
ON ClassSessions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT (UPDATE(ClassId) OR UPDATE(RoomId) OR UPDATE(ShiftId)
            OR UPDATE(SessionDate) OR UPDATE(LecturerId) OR UPDATE(Status))
        RETURN;

    -- LecturerId của buổi học phải là Lecturer
    IF UPDATE(LecturerId) AND EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.LecturerId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName <> 'Lecturer'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50014, N'INVALID_ROLE: Giảng viên của buổi học phải có vai trò Lecturer.', 1;
    END

    -- ④ Không xếp buổi Scheduled vào phòng Closed
    -- (chỉ kiểm tra buổi Scheduled: buổi Completed ở phòng đóng sau này vẫn cập nhật được)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Rooms r ON r.RoomId = i.RoomId
        WHERE r.Condition = 'Closed' AND i.Status = 'Scheduled'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50010, N'ROOM_CLOSED: Phòng đang đóng cửa (Closed), không thể xếp lịch.', 1;
    END

    -- ③ Buổi học (mới / đổi ca / đổi ngày) không được trùng lịch của bất kỳ
    --    học viên Active nào trong lớp
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Enrollments e1 ON e1.ClassId = i.ClassId AND e1.Status = 'Active'
        JOIN Enrollments e2 WITH (UPDLOCK, HOLDLOCK)
                            ON e2.StudentId = e1.StudentId
                           AND e2.ClassId  <> i.ClassId
                           AND e2.Status    = 'Active'
        JOIN ClassSessions s2 ON s2.ClassId     = e2.ClassId
                             AND s2.SessionDate = i.SessionDate
                             AND s2.ShiftId     = i.ShiftId
                             AND s2.Status     <> 'Cancelled'
        WHERE i.Status = 'Scheduled'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50012, N'CONFLICT_STUDENT_SESSION: Buổi học này trùng ca/ngày với lớp khác của một học viên đã ghi danh.', 1;
    END
END;
GO

/* ---------- Enrollments: vai trò học viên, ghi danh trùng lịch ---------- */
CREATE OR ALTER TRIGGER trg_Enrollments_Validate
ON Enrollments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT (UPDATE(StudentId) OR UPDATE(ClassId) OR UPDATE(Status)) RETURN;
    IF NOT EXISTS (SELECT 1 FROM inserted WHERE Status = 'Active') RETURN;

    -- StudentId phải là Student
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Users u ON u.UserId = i.StudentId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE i.Status = 'Active' AND r.RoleName <> 'Student'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50014, N'INVALID_ROLE: Người ghi danh phải có vai trò Student.', 1;
    END

    -- ③ Các buổi Scheduled sắp tới của lớp mới không được trùng (ngày + ca)
    --    với buổi nào của các lớp Active khác mà học viên đang học.
    --    UPDLOCK + HOLDLOCK: khóa dải theo StudentId, tránh 2 giao dịch song song
    --    cùng ghi danh 1 học viên vào 2 lớp trùng lịch.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions sNew ON sNew.ClassId     = i.ClassId
                               AND sNew.Status      = 'Scheduled'
                               AND sNew.SessionDate >= CAST(GETDATE() AS DATE)
        JOIN Enrollments e WITH (UPDLOCK, HOLDLOCK)
                            ON e.StudentId = i.StudentId
                           AND e.ClassId  <> i.ClassId
                           AND e.Status    = 'Active'
        JOIN ClassSessions sOld ON sOld.ClassId     = e.ClassId
                               AND sOld.SessionDate = sNew.SessionDate
                               AND sOld.ShiftId     = sNew.ShiftId
                               AND sOld.Status     <> 'Cancelled'
        WHERE i.Status = 'Active'
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50013, N'CONFLICT_STUDENT_ENROLL: Học viên đã có lớp học trùng ca và ngày với lớp này.', 1;
    END
END;
GO

/* ---------- Attendance: đúng học viên, đúng giảng viên, đúng buổi ---------- */
CREATE OR ALTER TRIGGER trg_Attendance_Validate
ON Attendance
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT (UPDATE(SessionId) OR UPDATE(StudentId) OR UPDATE(RecordedBy)
            OR UPDATE(UpdatedBy) OR UPDATE(Status)) RETURN;

    -- Học viên phải là Student và đã ghi danh lớp của buổi học (từ EnrollDate)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions s ON s.SessionId = i.SessionId
        JOIN Users u ON u.UserId = i.StudentId
        JOIN Roles r ON r.RoleId = u.RoleId
        WHERE r.RoleName <> 'Student'
           OR NOT EXISTS (
                SELECT 1 FROM Enrollments e
                WHERE e.StudentId = i.StudentId
                  AND e.ClassId   = s.ClassId
                  AND e.EnrollDate <= s.SessionDate
           )
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50015, N'ATTENDANCE_NOT_ENROLLED: Học viên chưa ghi danh lớp của buổi học này.', 1;
    END

    -- Người điểm danh / người sửa điểm danh phải là giảng viên của buổi học đó
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions s ON s.SessionId = i.SessionId
        WHERE s.LecturerId <> COALESCE(i.UpdatedBy, i.RecordedBy)
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50016, N'ATTENDANCE_FORBIDDEN: Chỉ giảng viên của buổi học mới được điểm danh.', 1;
    END

    -- Không điểm danh buổi đã hủy hoặc buổi chưa diễn ra
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ClassSessions s ON s.SessionId = i.SessionId
        WHERE s.Status = 'Cancelled' OR s.SessionDate > CAST(GETDATE() AS DATE)
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50017, N'ATTENDANCE_SESSION_INVALID: Không thể điểm danh buổi đã hủy hoặc chưa diễn ra.', 1;
    END
END;
GO

/* =====================================================================
   10. VIEWS & FUNCTIONS (phục vụ các màn hình theo FINAL_PROJECT_DOC)
   ===================================================================== */

-- Facility Manager / Admin: phòng nào đang có lớp, phòng nào trống, phòng nào đóng bảo trì
-- theo từng ca của 1 ngày bất kỳ.
CREATE OR ALTER FUNCTION fn_RoomShiftStatus (@Date DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT
        f.FacilityId, f.FacilityName,
        r.RoomId, r.RoomNumber, r.Condition,
        sh.ShiftId, sh.ShiftName, sh.StartTime, sh.EndTime,
        cs.SessionId, cs.ClassId, c.ClassName,
        cs.LecturerId, u.FullName AS LecturerName,
        CASE WHEN r.Condition = 'Closed'   THEN N'Closed for Maintenance'
             WHEN cs.SessionId IS NOT NULL THEN N'Occupied'
             ELSE N'Empty' END AS RoomStatus
    FROM Rooms r
    JOIN Facilities f ON f.FacilityId = r.FacilityId
    CROSS JOIN Shifts sh
    LEFT JOIN ClassSessions cs ON cs.RoomId      = r.RoomId
                              AND cs.ShiftId     = sh.ShiftId
                              AND cs.SessionDate = @Date
                              AND cs.Status     <> 'Cancelled'
    LEFT JOIN Classes c ON c.ClassId = cs.ClassId
    LEFT JOIN Users   u ON u.UserId  = cs.LecturerId
);
GO

-- Admin: thống kê tổng quan (1 dòng)
CREATE OR ALTER VIEW vw_AdminStats
AS
SELECT
    (SELECT COUNT(*) FROM Rooms)                                              AS TotalRooms,
    (SELECT COUNT(*) FROM Rooms WHERE Condition = 'Open')                     AS FunctioningRooms,
    (SELECT COUNT(*) FROM Rooms WHERE Condition = 'Closed')                   AS MaintenanceRooms,
    (SELECT COUNT(*) FROM Classes WHERE Status = 'Active')                    AS ActiveClasses,
    (SELECT COUNT(*) FROM Users u JOIN Roles r ON r.RoleId = u.RoleId
      WHERE r.RoleName = 'Lecturer' AND u.IsDeleted = 0 AND u.IsActive = 1)   AS ActiveLecturers,
    (SELECT COUNT(*) FROM Users u JOIN Roles r ON r.RoleId = u.RoleId
      WHERE r.RoleName = 'Student'  AND u.IsDeleted = 0 AND u.IsActive = 1)   AS ActiveStudents,
    (SELECT COUNT(*) FROM RoomChangeRequests WHERE Status = 'Pending')        AS PendingChangeRequests,
    (SELECT ISNULL(SUM(TotalAmount), 0) FROM Tuitions
      WHERE PaymentStatus = 'Unpaid')                                         AS UnpaidTuitionTotal;
GO

-- Grand Manager: số buổi đã đi học (Present) và học phí tạm tính = Present x đơn giá
-- trong khoảng [@From, @To], theo từng học viên + lớp.
CREATE OR ALTER FUNCTION fn_StudentAttendanceTuition (@From DATE, @To DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT
        a.StudentId,
        s.ClassId,
        SUM(CASE WHEN a.Status = 'Present'         THEN 1 ELSE 0 END) AS PresentDays,
        SUM(CASE WHEN a.Status = 'ExcusedAbsent'   THEN 1 ELSE 0 END) AS ExcusedAbsentDays,
        SUM(CASE WHEN a.Status = 'UnexcusedAbsent' THEN 1 ELSE 0 END) AS UnexcusedAbsentDays,
        SUM(CASE WHEN a.Status = 'Present'         THEN 1 ELSE 0 END) * rate.Amount AS TuitionAmount
    FROM Attendance a
    JOIN ClassSessions s ON s.SessionId = a.SessionId
    CROSS JOIN (
        SELECT CAST(SettingValue AS DECIMAL(12,0)) AS Amount
        FROM SystemSettings
        WHERE SettingKey = 'TUITION_PER_SESSION'
    ) rate
    WHERE s.SessionDate BETWEEN @From AND @To
    GROUP BY a.StudentId, s.ClassId, rate.Amount
);
GO

/* =====================================================================
   11. INDEXES (tối ưu truy vấn thường dùng)
   (UQ_Session đã phủ (ClassId, SessionDate); UX_Sessions_* phủ phòng / giảng viên)
   ===================================================================== */

CREATE INDEX IX_Users_RoleId            ON Users(RoleId);
CREATE INDEX IX_Classes_LecturerId      ON Classes(LecturerId);
CREATE INDEX IX_Classes_RoomShift       ON Classes(RoomId, ShiftId);
CREATE INDEX IX_Sessions_DateShift      ON ClassSessions(SessionDate, ShiftId);
CREATE INDEX IX_Attendance_StudentId    ON Attendance(StudentId);
CREATE INDEX IX_Enrollments_ClassId     ON Enrollments(ClassId);
CREATE INDEX IX_Tuitions_StudentStatus  ON Tuitions(StudentId, PaymentStatus);
CREATE INDEX IX_RCR_Status              ON RoomChangeRequests(Status);
GO

/* =====================================================================
   12. SEED DATA (dữ liệu khởi tạo)
   ===================================================================== */

-- Roles
INSERT INTO Roles (RoleName) VALUES
('Admin'), ('GrandManager'), ('FacilityManager'), ('Lecturer'), ('Student');
GO

-- Facilities (3 cơ sở) - ManagerId gán sau khi tạo tài khoản Facility Manager
INSERT INTO Facilities (FacilityName, Address) VALUES
(N'Cơ sở 1', N'Địa chỉ cơ sở 1'),
(N'Cơ sở 2', N'Địa chỉ cơ sở 2'),
(N'Cơ sở 3', N'Địa chỉ cơ sở 3');
GO

-- Rooms: 6 phòng / cơ sở = 18 phòng
DECLARE @f INT = 1;
WHILE @f <= 3
BEGIN
    DECLARE @r INT = 1;
    WHILE @r <= 6
    BEGIN
        INSERT INTO Rooms (FacilityId, RoomNumber, Capacity)
        VALUES (@f, CONCAT('P', @f, '0', @r), 30);
        SET @r += 1;
    END
    SET @f += 1;
END
GO

-- Shifts (3 ca theo timeline yêu cầu)
INSERT INTO Shifts (ShiftName, StartTime, EndTime) VALUES
(N'Ca 1', '04:30:00', '06:00:00'),
(N'Ca 2', '06:00:00', '07:30:00'),
(N'Ca 3', '07:30:00', '09:00:00');
GO

-- Subjects (5 môn)
INSERT INTO Subjects (SubjectName) VALUES
('Math'), ('Literature'), ('Physics'), ('English'), ('Chemistry');
GO

-- System Settings
INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('TUITION_PER_SESSION', '200000', N'Học phí mặc định mỗi buổi học (VNĐ)');
GO

-- Admin mặc định (PasswordHash là placeholder: hãy seed lại bằng PasswordHasher / BCrypt trong code C#)
INSERT INTO Users (FullName, Email, PasswordHash, RoleId, IsActive) VALUES
(N'System Admin', 'admin@tutorflow.local', 'CHANGE_ME_HASH', 1, 1);
GO
