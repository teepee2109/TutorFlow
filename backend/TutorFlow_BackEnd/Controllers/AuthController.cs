using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorFlow.BackEnd.Dtos;
using TutorFlow.Services.Auth;

namespace TutorFlow.BackEnd.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<UserAuthDto>>> Login(LoginRequest request)
    {
        var result = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (!result.Success || result.Value is null)
        {
            return Unauthorized(ApiResponse<UserAuthDto>.Fail(result.Message));
        }

        var user = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.RoleName)
        };
        if (user.FacilityId.HasValue)
        {
            claims.Add(new Claim("FacilityId", user.FacilityId.Value.ToString()));
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return Ok(ApiResponse<UserAuthDto>.Ok(user, result.Message));
    }
}
