namespace TutorFlow.Services.Auth;

public class UserAuthDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int? FacilityId { get; set; }
    public string? FacilityName { get; set; }
}
