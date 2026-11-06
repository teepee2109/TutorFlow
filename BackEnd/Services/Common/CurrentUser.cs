using System.Security.Claims;

namespace TutorFlow.Services.Common;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId => int.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? FullName => User?.FindFirst(ClaimTypes.Name)?.Value;
    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;
    public int? FacilityId => int.TryParse(User?.FindFirst("FacilityId")?.Value, out var id) ? id : null;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
