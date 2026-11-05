using Microsoft.EntityFrameworkCore;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Auth;
using TutorFlow.Services.Common;
using TutorFlow.Services.Users;
using Xunit;

namespace TutorFlow.Tests;

public class UserServiceTests
{
    private TutorFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TutorFlowDbContext(options);
    }

    [Fact]
    public async Task GrandManager_CannotCreate_AdminUser()
    {
        using var db = CreateInMemoryDbContext();
        var authService = new AuthService(db);
        var userService = new UserService(db, authService);

        var adminRole = new Role { RoleId = 1, RoleName = "Admin" };
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();

        var createDto = new UserCreateDto
        {
            FullName = "New Admin",
            Email = "newadmin@tutorflow.local",
            RoleId = 1,
            Password = "Password@123"
        };

        // GrandManager attempts to create Admin
        var result = await userService.CreateUserAsync(createDto, operatorRole: "GrandManager");

        Assert.False(result.Success);
        Assert.Equal(ScheduleError.InvalidRole, result.Error);
    }

    [Fact]
    public async Task CreateUser_WithExistingEmail_ReturnsValidationError()
    {
        using var db = CreateInMemoryDbContext();
        var authService = new AuthService(db);
        var userService = new UserService(db, authService);

        var studentRole = new Role { RoleId = 5, RoleName = "Student" };
        db.Roles.Add(studentRole);

        db.Users.Add(new User
        {
            UserId = 10,
            FullName = "Existing Student",
            Email = "student@tutorflow.local",
            PasswordHash = "hash",
            RoleId = 5,
            IsActive = true,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var createDto = new UserCreateDto
        {
            FullName = "Another Student",
            Email = "student@tutorflow.local",
            RoleId = 5,
            Password = "Password@123"
        };

        var result = await userService.CreateUserAsync(createDto, operatorRole: "Admin");

        Assert.False(result.Success);
        Assert.Contains("Email này đã được sử dụng", result.Message);
    }

    [Fact]
    public async Task SoftDelete_LecturerWithActiveClasses_IsPrevented()
    {
        using var db = CreateInMemoryDbContext();
        var authService = new AuthService(db);
        var userService = new UserService(db, authService);

        var lecturerRole = new Role { RoleId = 4, RoleName = "Lecturer" };
        db.Roles.Add(lecturerRole);

        var lecturer = new User
        {
            UserId = 20,
            FullName = "Active Lecturer",
            Email = "lecturer@tutorflow.local",
            PasswordHash = "hash",
            RoleId = 4,
            Role = lecturerRole,
            IsActive = true,
            IsDeleted = false
        };
        db.Users.Add(lecturer);

        // Active class assigned to lecturer
        db.Classes.Add(new Class
        {
            ClassId = 1,
            ClassName = "Toán 10",
            SubjectId = 1,
            LecturerId = 20,
            FacilityId = 1,
            RoomId = 1,
            ShiftId = 1,
            DaysOfWeek = "2,4,6",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var result = await userService.SoftDeleteAsync(userId: 20, operatorRole: "Admin");

        Assert.False(result.Success);
        Assert.Contains("Không thể xóa giảng viên đang có lớp học hoạt động", result.Message);

        // Verify user is not deleted
        var userInDb = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.UserId == 20);
        Assert.False(userInDb.IsDeleted);
    }
}
