using System.ComponentModel.DataAnnotations;

namespace TutorFlow.BackEnd.Dtos;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Value { get; init; }

    public static ApiResponse<T> Ok(T value, string message = "") => new()
    {
        Success = true,
        Message = message,
        Value = value
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class SetActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class ResetPasswordRequest
{
    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class AssignFacilityManagerRequest
{
    public int? ManagerId { get; set; }
}

public sealed class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public sealed class FacilityDto
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
    public int RoomCount { get; set; }
}
