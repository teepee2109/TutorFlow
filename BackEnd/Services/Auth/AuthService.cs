using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using BackEnd.Services.Common;

namespace BackEnd.Services.Auth;

public class AuthService : IAuthService
{
    private readonly TutorFlowDbContext _db;

    public AuthService(TutorFlowDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserAuthDto>> AuthenticateAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Result<UserAuthDto>.Fail("Vui lòng nhập đầy đủ Email và Mật khẩu.");
        }

        // Tìm người dùng chưa bị xóa mềm (HasQueryFilter u => !u.IsDeleted tự động áp dụng)
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email.Trim());

        if (user == null)
        {
            return Result<UserAuthDto>.Fail("Email hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            return Result<UserAuthDto>.Fail("Tài khoản của bạn đang bị tạm khóa. Vui lòng liên hệ Quản trị viên.");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return Result<UserAuthDto>.Fail("Email hoặc mật khẩu không chính xác.");
        }

        // Nếu là FacilityManager, tìm cơ sở mà user này đang phụ trách (BR-11)
        int? facilityId = null;
        string? facilityName = null;

        if (user.Role.RoleName == "FacilityManager")
        {
            var facility = await _db.Facilities
                .FirstOrDefaultAsync(f => f.ManagerId == user.UserId);

            if (facility != null)
            {
                facilityId = facility.FacilityId;
                facilityName = facility.FacilityName;
            }
        }

        var authDto = new UserAuthDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role.RoleName,
            FacilityId = facilityId,
            FacilityName = facilityName
        };

        return Result<UserAuthDto>.Ok(authDto, "Đăng nhập thành công.");
    }

    public string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 11);
    }

    public bool VerifyPassword(string plainPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
