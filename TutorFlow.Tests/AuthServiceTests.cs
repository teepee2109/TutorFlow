using Microsoft.EntityFrameworkCore;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Auth;
using TutorFlow.Services.Common;
using Xunit;

namespace TutorFlow.Tests;

public class AuthServiceTests
{
    private TutorFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TutorFlowDbContext(options);
    }

    [Fact]
    public void HashPassword_And_VerifyPassword_WorkCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var authService = new AuthService(db);

        var plain = "Test@123456";
        var hash = authService.HashPassword(plain);

        Assert.NotEmpty(hash);
        Assert.True(authService.VerifyPassword(plain, hash));
        Assert.False(authService.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsOk()
    {
        using var db = CreateInMemoryDbContext();
        var authService = new AuthService(db);

        var role = new Role { RoleId = 1, RoleName = "Admin" };
        db.Roles.Add(role);

        var user = new User
        {
            UserId = 1,
            FullName = "Admin User",
            Email = "admin@tutorflow.local",
            PasswordHash = authService.HashPassword("Dev@12345"),
            RoleId = 1,
            Role = role,
            IsActive = true,
            IsDeleted = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await authService.AuthenticateAsync("admin@tutorflow.local", "Dev@12345");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("Admin User", result.Value.FullName);
        Assert.Equal("Admin", result.Value.RoleName);
    }

    [Fact]
    public async Task AuthenticateAsync_WithLockedAccount_ReturnsFail()
    {
        using var db = CreateInMemoryDbContext();
        var authService = new AuthService(db);

        var role = new Role { RoleId = 2, RoleName = "Lecturer" };
        db.Roles.Add(role);

        var user = new User
        {
            UserId = 2,
            FullName = "Locked Lecturer",
            Email = "locked@tutorflow.local",
            PasswordHash = authService.HashPassword("Dev@12345"),
            RoleId = 2,
            Role = role,
            IsActive = false, // Locked
            IsDeleted = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await authService.AuthenticateAsync("locked@tutorflow.local", "Dev@12345");

        Assert.False(result.Success);
        Assert.Contains("tạm khóa", result.Message);
    }
}
