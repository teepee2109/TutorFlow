/*
    Correct the demo timetable to normal afternoon/evening lesson hours.
    This updates only the three lookup rows and preserves ShiftId values,
    so existing Classes and ClassSessions keep their current references.
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Shifts', N'U') IS NULL
        THROW 51010, N'Không tìm thấy bảng dbo.Shifts. Hãy chọn đúng database TutorFlow.', 1;

    DECLARE @ExpectedShifts TABLE
    (
        ShiftName NVARCHAR(50) NOT NULL PRIMARY KEY,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL
    );

    INSERT INTO @ExpectedShifts (ShiftName, StartTime, EndTime)
    VALUES
        (N'Ca 1', '16:30:00', '18:00:00'),
        (N'Ca 2', '18:00:00', '19:30:00'),
        (N'Ca 3', '19:30:00', '21:00:00');

    IF EXISTS
    (
        SELECT 1
        FROM @ExpectedShifts AS expected
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.Shifts AS currentShift
            WHERE currentShift.ShiftName = expected.ShiftName
        )
    )
        THROW 51011, N'Không tìm thấy đủ Ca 1, Ca 2 và Ca 3 trong dbo.Shifts; không có dữ liệu nào được cập nhật.', 1;

    UPDATE currentShift
    SET currentShift.StartTime = expected.StartTime,
        currentShift.EndTime = expected.EndTime
    FROM dbo.Shifts AS currentShift
    INNER JOIN @ExpectedShifts AS expected
        ON expected.ShiftName = currentShift.ShiftName;

    COMMIT TRANSACTION;

    SELECT ShiftId, ShiftName, StartTime, EndTime
    FROM dbo.Shifts
    WHERE ShiftName IN (N'Ca 1', N'Ca 2', N'Ca 3')
    ORDER BY StartTime;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
