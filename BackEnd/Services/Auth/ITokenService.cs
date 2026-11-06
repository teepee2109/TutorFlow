using TutorFlow.Services.Auth;

namespace TutorFlow.Services.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(UserAuthDto user, bool rememberMe);
}
