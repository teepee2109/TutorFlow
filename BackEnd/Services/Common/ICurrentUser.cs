namespace BackEnd.Services.Common;

/// <summary>
/// Cung cấp thông tin người dùng hiện tại đang đăng nhập
/// Được dùng trong các Service để kiểm tra quyền và phạm vi cơ sở (FacilityId)
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? Role { get; }
    int? FacilityId { get; }
    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
