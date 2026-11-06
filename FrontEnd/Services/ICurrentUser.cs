namespace FrontEnd.Services;

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
