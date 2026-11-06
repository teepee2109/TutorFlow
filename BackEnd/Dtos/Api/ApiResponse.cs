namespace BackEnd.Dtos.Api;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ApiResponse<T> Ok(T value, string message = "") => new()
    {
        Success = true,
        Value = value,
        Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public sealed class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ApiResponse Ok(string message = "") => new() { Success = true, Message = message };
    public static ApiResponse Fail(string message) => new() { Success = false, Message = message };
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public TutorFlow.Services.Auth.UserAuthDto User { get; init; } = new();
}

public sealed class UserActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class AssignFacilityManagerRequest
{
    public int? ManagerId { get; set; }
}

public sealed class RoleOptionDto
{
    public int RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
}

public sealed class FacilityItemDto
{
    public int FacilityId { get; init; }
    public string FacilityName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public int? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public string? ManagerEmail { get; init; }
    public int RoomCount { get; init; }
}

public sealed class FacilitiesDataDto
{
    public List<FacilityItemDto> Facilities { get; init; } = new();
    public List<TutorFlow.Services.Users.UserDto> EligibleManagers { get; init; } = new();
}

public sealed class AdminDashboardDto
{
    public int? TotalRooms { get; init; }
    public int? OpenRooms { get; init; }
    public int? ClosedRooms { get; init; }
    public int? ActiveClasses { get; init; }
    public int? ActiveLecturers { get; init; }
    public int? ActiveStudents { get; init; }
    public int? PendingChangeRequests { get; init; }
    public decimal TotalUnpaidTuition { get; init; }
}
