/* =====================================================================
   PROJECT   : EduCenterManagement (Hệ thống quản lý Trung tâm học tập)
   PLATFORM  : SQL Server (tương thích .NET 8 / EF Core)
   MÔ TẢ     : Database cho 5 vai trò - Admin, Grand Manager,
               Facility Manager, Lecturer, Student
   ===================================================================== */

/*CREATE DATABASE EduCenterManagement;
GO*/
USE EduCenterManagement;
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
    IsActive        BIT NOT NULL DEFAULT 1,           -- khóa/kích hoạt tài khoản
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
    Address         NVARCHAR(255) NULL
);
GO

CREATE TABLE Rooms (
    RoomId          INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId      INT NOT NULL,
    RoomNumber      NVARCHAR(20) NOT NULL,             -- vd: P101
    Capacity        INT NULL,
    Condition       NVARCHAR(20) NOT NULL DEFAULT 'Open'
                        CHECK (Condition IN ('Open','Closed')),  -- Open / Closed for Maintenance
    CONSTRAINT FK_Rooms_Facilities FOREIGN KEY (FacilityId) REFERENCES Facilities(FacilityId),
    CONSTRAINT UQ_Room_Facility UNIQUE (FacilityId, RoomNumber)
);
GO

/* =====================================================================
   3. SHIFTS (Ca học) & SUBJECTS (Môn học)
   ===================================================================== */

CREATE TABLE Shifts (
    ShiftId         INT IDENTITY(1,1) PRIMARY KEY,
    ShiftName       NVARCHAR(50) NOT NULL,             -- "Ca 1", "Ca 2", "Ca 3"
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
    LecturerId      INT NOT NULL,                       -- FK -> Users (role = Lecturer)
    FacilityId      INT NOT NULL,
    RoomId          INT NOT NULL,                        -- phòng mặc định của lớp
    ShiftId         INT NOT NULL,
    DaysOfWeek      NVARCHAR(20) NOT NULL,               -- vd: "2,4,6" = Thứ 2,4,6
    StartDate       DATE NOT NULL,
    EndDate         DATE NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Active'
                        CHECK (Status IN ('Active','Closed')),
    CreatedBy       INT NULL,                            -- Grand Manager tạo
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Classes_Subjects   FOREIGN KEY (SubjectId)  REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_Classes_Lecturer   FOREIGN KEY (LecturerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Classes_Facilities FOREIGN KEY (FacilityId) REFERENCES Facilities(FacilityId),
    CONSTRAINT FK_Classes_Rooms      FOREIGN KEY (RoomId)     REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Classes_Shifts     FOREIGN KEY (ShiftId)    REFERENCES Shifts(ShiftId),
    CONSTRAINT FK_Classes_CreatedBy  FOREIGN KEY (CreatedBy)  REFERENCES Users(UserId)
);
GO

-- Từng buổi học cụ thể của 1 lớp (dùng để điểm danh & tính học phí)
CREATE TABLE ClassSessions (
    SessionId       INT IDENTITY(1,1) PRIMARY KEY,
    ClassId         INT NOT NULL,
    SessionDate     DATE NOT NULL,
    RoomId          INT NOT NULL,                        -- có thể khác RoomId gốc nếu đổi phòng buổi đó
    ShiftId         INT NOT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Scheduled'
                        CHECK (Status IN ('Scheduled','Completed','Cancelled')),
    CONSTRAINT FK_Sessions_Classes FOREIGN KEY (ClassId) REFERENCES Classes(ClassId),
    CONSTRAINT FK_Sessions_Rooms   FOREIGN KEY (RoomId)  REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Sessions_Shifts  FOREIGN KEY (ShiftId) REFERENCES Shifts(ShiftId),
    CONSTRAINT UQ_Session UNIQUE (ClassId, SessionDate)
);
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
    UpdatedAt       DATETIME NULL,
    CONSTRAINT FK_Attendance_Sessions FOREIGN KEY (SessionId)  REFERENCES ClassSessions(SessionId),
    CONSTRAINT FK_Attendance_Students FOREIGN KEY (StudentId)  REFERENCES Users(UserId),
    CONSTRAINT FK_Attendance_Recorder FOREIGN KEY (RecordedBy) REFERENCES Users(UserId),
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
    CONSTRAINT FK_Tuitions_Classes  FOREIGN KEY (ClassId)   REFERENCES Classes(ClassId)
);
GO

CREATE TABLE Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    TuitionId       INT NOT NULL,
    AmountPaid      DECIMAL(12,0) NOT NULL,
    PaymentDate     DATETIME NOT NULL DEFAULT GETDATE(),
    PaymentMethod   NVARCHAR(50) NULL,                    -- Cash, Bank Transfer, ...
    ReceivedBy      INT NULL,                             -- Grand Manager xác nhận
    ReceiptNumber   NVARCHAR(50) NULL,
    CONSTRAINT FK_Payments_Tuitions FOREIGN KEY (TuitionId)  REFERENCES Tuitions(TuitionId),
    CONSTRAINT FK_Payments_Users    FOREIGN KEY (ReceivedBy) REFERENCES Users(UserId)
);
GO

/* =====================================================================
   7. ROOM / SHIFT CHANGE REQUESTS
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
   9. INDEXES (tối ưu truy vấn thường dùng)
   ===================================================================== */

CREATE INDEX IX_Users_RoleId            ON Users(RoleId);
CREATE INDEX IX_Classes_LecturerId      ON Classes(LecturerId);
CREATE INDEX IX_Classes_RoomShift       ON Classes(RoomId, ShiftId);
CREATE INDEX IX_Sessions_ClassDate      ON ClassSessions(ClassId, SessionDate);
CREATE INDEX IX_Sessions_RoomShiftDate  ON ClassSessions(RoomId, ShiftId, SessionDate);
CREATE INDEX IX_Attendance_StudentId    ON Attendance(StudentId);
CREATE INDEX IX_Enrollments_ClassId     ON Enrollments(ClassId);
CREATE INDEX IX_Tuitions_StudentStatus  ON Tuitions(StudentId, PaymentStatus);
CREATE INDEX IX_RCR_Status              ON RoomChangeRequests(Status);
GO

/* =====================================================================
   10. SEED DATA (dữ liệu khởi tạo)
   ===================================================================== */

-- Roles
INSERT INTO Roles (RoleName) VALUES
('Admin'), ('GrandManager'), ('FacilityManager'), ('Lecturer'), ('Student');
GO

-- Facilities (3 cơ sở)
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

-- Admin mặc định (mật khẩu cần hash lại khi code, đây chỉ là placeholder)
INSERT INTO Users (FullName, Email, PasswordHash, RoleId, IsActive) VALUES
(N'System Admin', 'admin@educenter.local', 'CHANGE_ME_HASH', 1, 1);
GO