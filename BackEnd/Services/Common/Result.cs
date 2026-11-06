namespace BackEnd.Services.Common;

/// <summary>
/// Đại diện cho kết quả thực thi thao tác không có giá trị trả về
/// </summary>
public class Result
{
    public bool Success { get; init; }
    public ScheduleError? Error { get; init; }
    public string Message { get; init; } = string.Empty;

    public static Result Ok(string message = "") => new()
    {
        Success = true,
        Message = message
    };

    public static Result Fail(ScheduleError error, string message) => new()
    {
        Success = false,
        Error = error,
        Message = message
    };

    public static Result Fail(string message) => new()
    {
        Success = false,
        Error = ScheduleError.ValidationError,
        Message = message
    };
}

/// <summary>
/// Đại diện cho kết quả thực thi thao tác có giá trị trả về
/// </summary>
public sealed class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Ok(T value, string message = "") => new()
    {
        Success = true,
        Value = value,
        Message = message
    };

    public new static Result<T> Fail(ScheduleError error, string message) => new()
    {
        Success = false,
        Error = error,
        Message = message
    };

    public new static Result<T> Fail(string message) => new()
    {
        Success = false,
        Error = ScheduleError.ValidationError,
        Message = message
    };
}
