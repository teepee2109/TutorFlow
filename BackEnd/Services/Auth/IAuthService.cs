using BackEnd.Services.Common;

namespace BackEnd.Services.Auth;

public interface IAuthService
{
    Task<Result<UserAuthDto>> AuthenticateAsync(string email, string password);
    string HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, string passwordHash);
}
