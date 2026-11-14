/* Safe existing-database upgrade. Preserves issued amounts and payments. Run once in SSMS. */
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
BEGIN TRY
 BEGIN TRANSACTION;
 IF COL_LENGTH(N'dbo.Tuitions',N'SessionsAttended') IS NOT NULL AND COL_LENGTH(N'dbo.Tuitions',N'BillableSessions') IS NULL
 BEGIN
  IF COL_LENGTH(N'dbo.Tuitions',N'TotalAmount') IS NOT NULL ALTER TABLE dbo.Tuitions DROP COLUMN TotalAmount;
  EXEC sys.sp_rename N'dbo.Tuitions.SessionsAttended',N'BillableSessions',N'COLUMN';
  ALTER TABLE dbo.Tuitions ADD TotalAmount AS (BillableSessions*AmountPerSession) PERSISTED;
 END;
 DECLARE @constraint sysname;
 SELECT TOP(1) @constraint=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Tuitions') AND definition LIKE N'%PaymentStatus%';
 IF @constraint IS NOT NULL
 BEGIN
  DECLARE @dropConstraintSql nvarchar(max) = N'ALTER TABLE dbo.Tuitions DROP CONSTRAINT ' + QUOTENAME(@constraint);
  EXEC sys.sp_executesql @dropConstraintSql;
 END;
 UPDATE t SET PaymentStatus=CASE WHEN p.Paid>=ISNULL(t.TotalAmount,0) THEN N'Paid' WHEN p.Paid>0 THEN N'PartiallyPaid' ELSE N'Unpaid' END
 FROM dbo.Tuitions t OUTER APPLY(SELECT ISNULL(SUM(AmountPaid),0) Paid FROM dbo.Payments WHERE TuitionId=t.TuitionId)p;
 ALTER TABLE dbo.Tuitions ADD CONSTRAINT CK_Tuitions_PaymentStatus CHECK(PaymentStatus IN(N'Unpaid',N'PartiallyPaid',N'Paid'));
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
CREATE OR ALTER FUNCTION dbo.fn_StudentAttendanceTuition(@PeriodFrom DATE,@PeriodTo DATE)
RETURNS TABLE AS RETURN
(
 SELECT a.StudentId,u.FullName AS StudentName,cs.ClassId,c.ClassName,
 SUM(CASE WHEN a.Status='Present' THEN 1 ELSE 0 END) PresentCount,
 SUM(CASE WHEN a.Status='ExcusedAbsent' THEN 1 ELSE 0 END) ExcusedCount,
 SUM(CASE WHEN a.Status='UnexcusedAbsent' THEN 1 ELSE 0 END) UnexcusedCount,
 SUM(CASE WHEN a.Status IN('Present','UnexcusedAbsent') THEN 1 ELSE 0 END) BillableSessions,
 COUNT(a.AttendanceId) TotalSessions
 FROM dbo.ClassSessions cs JOIN dbo.Classes c ON c.ClassId=cs.ClassId
 JOIN dbo.Attendance a ON a.SessionId=cs.SessionId JOIN dbo.Users u ON u.UserId=a.StudentId
 WHERE cs.SessionDate BETWEEN @PeriodFrom AND @PeriodTo AND cs.Status='Completed'
 GROUP BY a.StudentId,u.FullName,cs.ClassId,c.ClassName
);
GO

IF OBJECT_ID(N'dbo.PaymentRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentRequests
    (
        PaymentRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentRequests PRIMARY KEY,
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
        CONSTRAINT FK_PaymentRequests_Tuitions FOREIGN KEY (TuitionId) REFERENCES dbo.Tuitions(TuitionId),
        CONSTRAINT FK_PaymentRequests_Students FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_PaymentRequests_Reviewers FOREIGN KEY (ReviewedBy) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_PaymentRequests_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(PaymentId)
    );
    CREATE INDEX IX_PaymentRequests_TuitionStatus ON dbo.PaymentRequests(TuitionId, Status);
    CREATE UNIQUE INDEX UX_PaymentRequests_PaymentId ON dbo.PaymentRequests(PaymentId) WHERE PaymentId IS NOT NULL;
END;
GO
