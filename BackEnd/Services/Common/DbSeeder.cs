using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using TutorFlow.Services.Auth;

namespace TutorFlow.Services.Common;

public class DbSeeder
{
    private readonly TutorFlowDbContext _db;
    private readonly IAuthService _authService;

    public const string DefaultDevPassword = "Dev@12345";

    public DbSeeder(TutorFlowDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task SeedAsync()
    {
        var passwordHash = _authService.HashPassword(DefaultDevPassword);

        // 1. Cập nhật mật khẩu chuẩn BCrypt cho Admin
        var admin = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "admin@tutorflow.local");
        if (admin != null)
        {
            admin.PasswordHash = passwordHash;
            admin.IsActive = true;
            admin.IsDeleted = false;
        }

        // Lấy danh sách Roles
        var roles = await _db.Roles.ToDictionaryAsync(r => r.RoleName, r => r.RoleId);

        // 2. Seed Grand Manager
        if (!await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "gm@tutorflow.local"))
        {
            _db.Users.Add(new User
            {
                FullName = "Grand Manager",
                Email = "gm@tutorflow.local",
                PasswordHash = passwordHash,
                RoleId = roles["GrandManager"],
                IsActive = true,
                IsDeleted = false
            });
        }

        await _db.SaveChangesAsync();

        // 3. Seed 3 Facility Managers và gán vào 3 cơ sở (BR-11)
        for (int i = 1; i <= 3; i++)
        {
            var email = $"fm{i}@tutorflow.local";
            var fm = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
            if (fm == null)
            {
                fm = new User
                {
                    FullName = $"Facility Manager Cơ Sở {i}",
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleId = roles["FacilityManager"],
                    IsActive = true,
                    IsDeleted = false
                };
                _db.Users.Add(fm);
                await _db.SaveChangesAsync();
            }

            var facility = await _db.Facilities.FirstOrDefaultAsync(f => f.FacilityId == i);
            if (facility != null && facility.ManagerId != fm.UserId)
            {
                facility.ManagerId = fm.UserId;
            }
        }
        await _db.SaveChangesAsync();

        // 4. Seed Giảng viên (Lecturers)
        string[] lecturerNames = ["Nguyễn Văn Toán", "Trần Thị Văn", "Lê Văn Lý"];
        for (int i = 1; i <= lecturerNames.Length; i++)
        {
            var email = $"lecturer{i}@tutorflow.local";
            if (!await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
            {
                _db.Users.Add(new User
                {
                    FullName = lecturerNames[i - 1],
                    Email = email,
                    PhoneNumber = $"090123450{i}",
                    PasswordHash = passwordHash,
                    RoleId = roles["Lecturer"],
                    IsActive = true,
                    IsDeleted = false
                });
            }
        }

        // 5. Seed Học viên (Students)
        string[] studentNames = ["Học Viên An", "Học Viên Bình", "Học Viên Cường", "Học Viên Dung", "Học Viên Em"];
        for (int i = 1; i <= studentNames.Length; i++)
        {
            var email = $"student{i}@tutorflow.local";
            if (!await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
            {
                _db.Users.Add(new User
                {
                    FullName = studentNames[i - 1],
                    Email = email,
                    PhoneNumber = $"098765430{i}",
                    PasswordHash = passwordHash,
                    RoleId = roles["Student"],
                    IsActive = true,
                    IsDeleted = false
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
