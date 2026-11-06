using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Dtos.Api;
using BackEnd.Services.Auth;

namespace BackEnd.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (!result.Success || result.Value == null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail(result.Message));
        }

        var token = _tokenService.CreateAccessToken(result.Value, request.RememberMe);
        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = result.Value
        }));
    }
}
