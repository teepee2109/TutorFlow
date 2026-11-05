using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TutorFlow.Services.Users;

namespace TutorFlow.Web.Models;

public class UserListViewModel
{
    public UserPagedResult UsersResult { get; set; } = new();
    public string? RoleFilter { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int CurrentPage { get; set; } = 1;
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

public class FacilityItemViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
    public int RoomCount { get; set; }
}
