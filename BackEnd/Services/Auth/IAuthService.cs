using TutorFlow.Services.Common;

namespace TutorFlow.Services.Auth;

public interface IAuthService
{
    Task<Result<UserAuthDto>> AuthenticateAsync(string email, string password);
    string HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, string passwordHash);
}
