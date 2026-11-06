using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TutorFlow.Web.Models;

public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Value { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public AuthenticatedUserDto User { get; set; } = new();
}

public sealed class AuthenticatedUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int? FacilityId { get; set; }
    public string? FacilityName { get; set; }
}

public sealed class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? AssignedFacilityId { get; set; }
    public string? AssignedFacilityName { get; set; }
}

public sealed class UserPagedResult
{
    public List<UserDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed class UserCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public string Password { get; set; } = string.Empty;
}

public sealed class UserUpdateDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
}

public sealed class RoleOptionDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public sealed class FacilityOptionViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
}

public sealed class FacilityItemViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
    public int RoomCount { get; set; }
}

public sealed class FacilitiesDataDto
{
    public List<FacilityItemViewModel> Facilities { get; set; } = new();
    public List<UserDto> EligibleManagers { get; set; } = new();
}

public sealed class AdminDashboardViewModel
{
    public DateOnly TargetDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public int? TotalRooms { get; set; }
    public int? OpenRooms { get; set; }
    public int? ClosedRooms { get; set; }
    public int? ActiveClasses { get; set; }
    public int? ActiveLecturers { get; set; }
    public int? ActiveStudents { get; set; }
    public int? PendingChangeRequests { get; set; }
    public decimal TotalUnpaidTuition { get; set; }
    public List<RoomShiftStatusViewModel> RoomShiftStatuses { get; set; } = new();
}

public sealed class RoomShiftStatusViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string RoomCondition { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string SlotStatus { get; set; } = string.Empty;
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? LecturerId { get; set; }
    public string? LecturerName { get; set; }
    public int? SessionId { get; set; }
}

public class UserListViewModel
{
    public UserPagedResult UsersResult { get; set; } = new();
    public string? RoleFilter { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int? FacilityId { get; set; }
    public int CurrentPage { get; set; } = 1;
    public List<SelectListItem> FacilityOptions { get; set; } = new();
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Họ và Tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Vai trò.")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu khởi tạo.")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    public string Password { get; set; } = "Dev@12345";

    public SelectList? RolesSelectList { get; set; }
}

public class UserEditViewModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Họ và Tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Vai trò.")]
    public int RoleId { get; set; }

    public SelectList? RolesSelectList { get; set; }
}

public class FacilitiesViewModel
{
    public List<FacilityItemViewModel> FacilitiesList { get; set; } = new();
    public List<UserDto> EligibleFMs { get; set; } = new();
}
