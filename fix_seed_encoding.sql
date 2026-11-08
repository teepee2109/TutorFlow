/* Repairs Vietnamese text corrupted when UTF-8 seed files were read with a legacy code page. */
USE TutorFlow;
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

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ExtraClassPrefix NVARCHAR(40) =
        NCHAR(76)+NCHAR(7899)+NCHAR(112)+NCHAR(32)+NCHAR(109)+NCHAR(7851)+NCHAR(117)+NCHAR(32)+
        NCHAR(98)+NCHAR(7893)+NCHAR(32)+NCHAR(115)+NCHAR(117)+NCHAR(110)+NCHAR(103)+NCHAR(32);
    DECLARE @ExtraFacilityPrefix NVARCHAR(40) =
        NCHAR(67)+NCHAR(417)+NCHAR(32)+NCHAR(115)+NCHAR(7903)+NCHAR(32)+NCHAR(98)+NCHAR(7893)+
        NCHAR(32)+NCHAR(115)+NCHAR(117)+NCHAR(110)+NCHAR(103)+NCHAR(32);
    DECLARE @ExtraAddressPrefix NVARCHAR(80) =
        NCHAR(272)+NCHAR(7883)+NCHAR(97)+NCHAR(32)+NCHAR(99)+NCHAR(104)+NCHAR(7881)+NCHAR(32)+
        NCHAR(109)+NCHAR(7851)+NCHAR(117)+NCHAR(32)+NCHAR(99)+NCHAR(417)+NCHAR(32)+NCHAR(115)+
        NCHAR(7903)+NCHAR(32)+NCHAR(98)+NCHAR(7893)+NCHAR(32)+NCHAR(115)+NCHAR(117)+NCHAR(110)+
        NCHAR(103)+NCHAR(32);
    DECLARE @StudentClassPrefix NVARCHAR(40) =
        NCHAR(72)+NCHAR(7885)+NCHAR(99)+NCHAR(32)+NCHAR(118)+NCHAR(105)+NCHAR(234)+NCHAR(110)+
        NCHAR(32)+NCHAR(108)+NCHAR(7899)+NCHAR(112)+NCHAR(32);
    DECLARE @StudentExtraPrefix NVARCHAR(40) =
        NCHAR(72)+NCHAR(7885)+NCHAR(99)+NCHAR(32)+NCHAR(118)+NCHAR(105)+NCHAR(234)+NCHAR(110)+
        NCHAR(32)+NCHAR(98)+NCHAR(7893)+NCHAR(32)+NCHAR(115)+NCHAR(117)+NCHAR(110)+NCHAR(103)+NCHAR(32);
    DECLARE @AttendanceNote NVARCHAR(100) =
        NCHAR(68)+NCHAR(7919)+NCHAR(32)+NCHAR(108)+NCHAR(105)+NCHAR(7879)+NCHAR(117)+NCHAR(32)+
        NCHAR(273)+NCHAR(105)+NCHAR(7875)+NCHAR(109)+NCHAR(32)+NCHAR(100)+NCHAR(97)+NCHAR(110)+
        NCHAR(104)+NCHAR(32)+NCHAR(109)+NCHAR(7851)+NCHAR(117);
    DECLARE @RequestReason NVARCHAR(200) =
        NCHAR(89)+NCHAR(234)+NCHAR(117)+NCHAR(32)+NCHAR(99)+NCHAR(7847)+NCHAR(117)+NCHAR(32)+
        NCHAR(273)+NCHAR(7893)+NCHAR(105)+NCHAR(32)+NCHAR(112)+NCHAR(104)+NCHAR(242)+NCHAR(110)+
        NCHAR(103)+NCHAR(47)+NCHAR(99)+NCHAR(97)+NCHAR(32)+NCHAR(109)+NCHAR(7851)+NCHAR(117)+
        NCHAR(32)+NCHAR(273)+NCHAR(7875)+NCHAR(32)+NCHAR(107)+NCHAR(105)+NCHAR(7875)+NCHAR(109)+
        NCHAR(32)+NCHAR(116)+NCHAR(104)+NCHAR(7917);
    DECLARE @ReviewNote NVARCHAR(100) =
        NCHAR(272)+NCHAR(227)+NCHAR(32)+NCHAR(120)+NCHAR(7917)+NCHAR(32)+NCHAR(108)+NCHAR(253)+
        NCHAR(32)+NCHAR(121)+NCHAR(234)+NCHAR(117)+NCHAR(32)+NCHAR(99)+NCHAR(7847)+NCHAR(117)+
        NCHAR(32)+NCHAR(109)+NCHAR(7851)+NCHAR(117);

    UPDATE facility
    SET FacilityName = @ExtraFacilityPrefix + RIGHT(CONVERT(NVARCHAR(10), TRY_CONVERT(INT, RIGHT(facility.FacilityName, 2)) + 100), 2),
        Address = @ExtraAddressPrefix + RIGHT(CONVERT(NVARCHAR(10), TRY_CONVERT(INT, RIGHT(facility.FacilityName, 2)) + 100), 2) + N', TP.HCM'
    FROM dbo.Facilities AS facility
    WHERE CHARINDEX(NCHAR(198)+NCHAR(161), facility.FacilityName COLLATE Latin1_General_100_BIN2) > 0;

    UPDATE sampleClass
    SET ClassName = @ExtraClassPrefix + RIGHT(sampleClass.ClassName, 2)
    FROM dbo.Classes AS sampleClass
    WHERE CHARINDEX(NCHAR(225)+NCHAR(187)+NCHAR(8250), sampleClass.ClassName COLLATE Latin1_General_100_BIN2) > 0;

    UPDATE student
    SET FullName = @StudentClassPrefix + SUBSTRING(student.Email, 14, 2) + N' - ' + SUBSTRING(student.Email, 17, 2)
    FROM dbo.Users AS student
    WHERE student.Email LIKE N'student[_]demo[_]%'
      AND CHARINDEX(NCHAR(225)+NCHAR(187), student.FullName COLLATE Latin1_General_100_BIN2) > 0;

    UPDATE student
    SET FullName = @StudentExtraPrefix + parts.ClassPart + N' - ' + parts.SlotPart
    FROM dbo.Users AS student
    CROSS APPLY
    (
        SELECT CHARINDEX(N'_', student.Email, 15) AS SeparatorPosition,
               CHARINDEX(N'@', student.Email) AS AtPosition
    ) AS positions
    CROSS APPLY
    (
        SELECT SUBSTRING(student.Email, 15, positions.SeparatorPosition - 15) AS ClassPart,
               SUBSTRING(student.Email, positions.SeparatorPosition + 1,
                   positions.AtPosition - positions.SeparatorPosition - 1) AS SlotPart
    ) AS parts
    WHERE student.Email LIKE N'student[_]class[_]%'
      AND CHARINDEX(NCHAR(225)+NCHAR(187), student.FullName COLLATE Latin1_General_100_BIN2) > 0;

    UPDATE attendance
    SET Note = @AttendanceNote
    FROM dbo.Attendance AS attendance
    WHERE CHARINDEX(NCHAR(225)+NCHAR(187), attendance.Note COLLATE Latin1_General_100_BIN2) > 0;

    UPDATE request
    SET Reason = @RequestReason
    FROM dbo.RoomChangeRequests AS request
    WHERE CHARINDEX(NCHAR(225)+NCHAR(187), request.Reason COLLATE Latin1_General_100_BIN2) > 0
       OR CHARINDEX(NCHAR(225)+NCHAR(186), request.Reason COLLATE Latin1_General_100_BIN2) > 0;

    UPDATE request
    SET ReviewNote = @ReviewNote
    FROM dbo.RoomChangeRequests AS request
    WHERE request.ReviewNote IS NOT NULL
      AND (CHARINDEX(NCHAR(225)+NCHAR(187), request.ReviewNote COLLATE Latin1_General_100_BIN2) > 0
        OR CHARINDEX(NCHAR(225)+NCHAR(186), request.ReviewNote COLLATE Latin1_General_100_BIN2) > 0);

    IF EXISTS
    (
        SELECT 1 FROM dbo.Classes
        WHERE CHARINDEX(NCHAR(225)+NCHAR(187)+NCHAR(8250), ClassName COLLATE Latin1_General_100_BIN2) > 0
    ) OR EXISTS
    (
        SELECT 1 FROM dbo.Facilities
        WHERE CHARINDEX(NCHAR(198)+NCHAR(161), FacilityName COLLATE Latin1_General_100_BIN2) > 0
    ) OR EXISTS
    (
        SELECT 1 FROM dbo.Users
        WHERE (Email LIKE N'student[_]demo[_]%' OR Email LIKE N'student[_]class[_]%')
          AND CHARINDEX(NCHAR(225)+NCHAR(187), FullName COLLATE Latin1_General_100_BIN2) > 0
    ) OR EXISTS
    (
        SELECT 1 FROM dbo.Attendance
        WHERE CHARINDEX(NCHAR(225)+NCHAR(187), Note COLLATE Latin1_General_100_BIN2) > 0
    ) OR EXISTS
    (
        SELECT 1 FROM dbo.RoomChangeRequests
        WHERE CHARINDEX(NCHAR(225)+NCHAR(187), Reason COLLATE Latin1_General_100_BIN2) > 0
           OR CHARINDEX(NCHAR(225)+NCHAR(186), Reason COLLATE Latin1_General_100_BIN2) > 0
           OR CHARINDEX(NCHAR(225)+NCHAR(187), ReviewNote COLLATE Latin1_General_100_BIN2) > 0
           OR CHARINDEX(NCHAR(225)+NCHAR(186), ReviewNote COLLATE Latin1_General_100_BIN2) > 0
    )
    BEGIN
        THROW 51006, 'Encoding repair verification failed.', 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'Unicode repair completed' AS Result;
