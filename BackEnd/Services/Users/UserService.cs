using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Data.Entities;
using TutorFlow.Services.Auth;
using TutorFlow.Services.Common;

namespace TutorFlow.Services.Users;

public class UserService : IUserService
{
    private readonly TutorFlowDbContext _db;
    private readonly IAuthService _authService;

    public UserService(TutorFlowDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task<Result<UserPagedResult>> GetUsersAsync(
        string? roleFilter = null,
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _db.Users
                .Include(u => u.Role)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(u => u.Role.RoleName == roleFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(u => u.FullName.Contains(s) || u.Email.Contains(s) || (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var facilities = await _db.Facilities.AsNoTracking().ToListAsync();

            var dtos = users.Select(u =>
            {
                var assignedFacility = facilities.FirstOrDefault(f => f.ManagerId == u.UserId);
                return new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RoleId = u.RoleId,
                    RoleName = u.Role.RoleName,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    AssignedFacilityId = assignedFacility?.FacilityId,
                    AssignedFacilityName = assignedFacility?.FacilityName
                };
            }).ToList();

            return Result<UserPagedResult>.Ok(new UserPagedResult
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            var mapped = SqlExceptionMapper.Map(ex);
            return Result<UserPagedResult>.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? ex.Message);
        }
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(int id)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return Result<UserDto>.Fail(ScheduleError.NotFound, "Không tìm thấy người dùng.");

        var facility = await _db.Facilities.FirstOrDefaultAsync(f => f.ManagerId == user.UserId);

        return Result<UserDto>.Ok(new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleId = user.RoleId,
            RoleName = user.Role.RoleName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            AssignedFacilityId = facility?.FacilityId,
            AssignedFacilityName = facility?.FacilityName
        });
    }

    public async Task<Result<UserDto>> CreateUserAsync(UserCreateDto dto, string operatorRole)
    {
        try
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == dto.RoleId);
            if (role == null)
            {
                return Result<UserDto>.Fail(ScheduleError.InvalidRole, "Vai trò không hợp lệ.");
            }

            // Phân quyền tạo tài khoản (F1):
            // Admin được tạo mọi vai trò.
            // GrandManager chỉ được tạo FacilityManager, Lecturer, Student.
            if (operatorRole == "GrandManager")
            {
                if (role.RoleName == "Admin" || role.RoleName == "GrandManager")
                {
                    return Result<UserDto>.Fail(ScheduleError.InvalidRole, "Grand Manager không có quyền tạo tài khoản Admin hoặc Grand Manager.");
                }
            }

            // Kiểm tra trùng Email kể cả với người dùng đã xóa mềm (IgnoreQueryFilters)
            var emailExists = await _db.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());

            if (emailExists)
            {
                return Result<UserDto>.Fail(ScheduleError.ValidationError, "Email này đã được sử dụng trong hệ thống.");
            }

            var newUser = new User
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                RoleId = dto.RoleId,
                PasswordHash = _authService.HashPassword(dto.Password),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            return Result<UserDto>.Ok(new UserDto
            {
                UserId = newUser.UserId,
                FullName = newUser.FullName,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                RoleId = newUser.RoleId,
                RoleName = role.RoleName,
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt
            }, "Tạo tài khoản thành công.");
        }
        catch (Exception ex)
        {
            var mapped = SqlExceptionMapper.Map(ex);
            return Result<UserDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? ex.Message);
        }
    }

    public async Task<Result<UserDto>> UpdateUserAsync(UserUpdateDto dto, string operatorRole)
    {
        try
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

            if (user == null)
                return Result<UserDto>.Fail(ScheduleError.NotFound, "Không tìm thấy người dùng.");

            // Grand Manager không được sửa Admin hoặc Grand Manager khác
            if (operatorRole == "GrandManager")
            {
                if (user.Role.RoleName == "Admin" || user.Role.RoleName == "GrandManager")
                {
                    return Result<UserDto>.Fail(ScheduleError.InvalidRole, "Bạn không có quyền chỉnh sửa tài khoản Admin hoặc Grand Manager.");
                }
            }

            var newRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == dto.RoleId);
            if (newRole == null)
            {
                return Result<UserDto>.Fail(ScheduleError.InvalidRole, "Vai trò không hợp lệ.");
            }

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim();
            user.RoleId = dto.RoleId;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return Result<UserDto>.Ok(new UserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleId = user.RoleId,
                RoleName = newRole.RoleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            }, "Cập nhật tài khoản thành công.");
        }
        catch (Exception ex)
        {
            var mapped = SqlExceptionMapper.Map(ex);
            return Result<UserDto>.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? ex.Message);
        }
    }

    public async Task<Result> ToggleActiveAsync(int userId, bool isActive, string operatorRole)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy người dùng.");

        if (operatorRole == "GrandManager" && (user.Role.RoleName == "Admin" || user.Role.RoleName == "GrandManager"))
        {
            return Result.Fail(ScheduleError.InvalidRole, "Bạn không có quyền khóa hoặc mở tài khoản cấp quản trị.");
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Result.Ok(isActive ? "Đã mở khóa tài khoản thành công." : "Đã tạm khóa tài khoản thành công.");
    }

    public async Task<Result> SoftDeleteAsync(int userId, string operatorRole)
    {
        try
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return Result.Fail(ScheduleError.NotFound, "Không tìm thấy người dùng.");

            if (operatorRole == "GrandManager" && (user.Role.RoleName == "Admin" || user.Role.RoleName == "GrandManager"))
            {
                return Result.Fail(ScheduleError.InvalidRole, "Bạn không có quyền xóa tài khoản cấp quản trị.");
            }

            // BR-09 & F1: Chặn xóa Lecturer còn lớp Active
            if (user.Role.RoleName == "Lecturer")
            {
                var hasActiveClasses = await _db.Classes.AnyAsync(c => c.LecturerId == userId && c.Status == "Active");
                if (hasActiveClasses)
                {
                    return Result.Fail(ScheduleError.ValidationError, "Không thể xóa giảng viên đang có lớp học hoạt động. Vui lòng bàn giao lớp trước.");
                }
            }

            // BR-09 & F1: Chặn xóa FacilityManager đang phụ trách cơ sở
            if (user.Role.RoleName == "FacilityManager")
            {
                var managesFacility = await _db.Facilities.AnyAsync(f => f.ManagerId == userId);
                if (managesFacility)
                {
                    return Result.Fail(ScheduleError.ValidationError, "Không thể xóa Facility Manager đang phụ trách cơ sở. Vui lòng chuyển giao cơ sở trước.");
                }
            }

            // Xóa mềm: IsDeleted = 1 (BR-09)
            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return Result.Ok("Xóa người dùng thành công.");
        }
        catch (Exception ex)
        {
            var mapped = SqlExceptionMapper.Map(ex);
            return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? ex.Message);
        }
    }

    public async Task<Result> ResetPasswordAsync(int userId, string newPassword, string operatorRole)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return Result.Fail(ScheduleError.NotFound, "Không tìm thấy người dùng.");

        if (operatorRole == "GrandManager" && (user.Role.RoleName == "Admin" || user.Role.RoleName == "GrandManager"))
        {
            return Result.Fail(ScheduleError.InvalidRole, "Bạn không có quyền đổi mật khẩu tài khoản cấp quản trị.");
        }

        user.PasswordHash = _authService.HashPassword(newPassword);
        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Result.Ok("Đã đặt lại mật khẩu thành công.");
    }

    public async Task<Result> AssignFacilityManagerAsync(int facilityId, int? managerId, string operatorRole)
    {
        try
        {
            // Chỉ Admin mới được phân bổ Facility Manager cho cơ sở
            if (operatorRole != "Admin")
            {
                return Result.Fail(ScheduleError.InvalidRole, "Chỉ Quản trị viên (Admin) mới có quyền phân công người phụ trách cơ sở.");
            }

            var facility = await _db.Facilities.FirstOrDefaultAsync(f => f.FacilityId == facilityId);
            if (facility == null)
            {
                return Result.Fail(ScheduleError.NotFound, "Không tìm thấy cơ sở.");
            }

            if (managerId.HasValue)
            {
                var manager = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == managerId.Value);

                if (manager == null)
                {
                    return Result.Fail(ScheduleError.NotFound, "Không tìm thấy người dùng.");
                }

                if (manager.Role.RoleName != "FacilityManager")
                {
                    return Result.Fail(ScheduleError.InvalidRole, "Người được chọn phải có vai trò FacilityManager.");
                }

                // BR-11: Mỗi Facility Manager phụ trách tối đa 1 cơ sở
                var currentFacility = await _db.Facilities
                    .FirstOrDefaultAsync(f => f.ManagerId == managerId.Value && f.FacilityId != facilityId);

                if (currentFacility != null)
                {
                    return Result.Fail(ScheduleError.InvalidRole, $"Người dùng này đã đang phụ trách {currentFacility.FacilityName}. Mỗi Facility Manager chỉ được phụ trách tối đa 1 cơ sở.");
                }
            }

            facility.ManagerId = managerId;
            await _db.SaveChangesAsync();

            return Result.Ok("Phân công người phụ trách cơ sở thành công.");
        }
        catch (Exception ex)
        {
            var mapped = SqlExceptionMapper.Map(ex);
            return Result.Fail(mapped?.Error ?? ScheduleError.ValidationError, mapped?.Message ?? ex.Message);
        }
    }

    public async Task<List<UserDto>> GetEligibleFacilityManagersAsync()
    {
        var fms = await _db.Users
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "FacilityManager" && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var facilities = await _db.Facilities.ToListAsync();

        return fms.Select(u =>
        {
            var fac = facilities.FirstOrDefault(f => f.ManagerId == u.UserId);
            return new UserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role.RoleName,
                IsActive = u.IsActive,
                AssignedFacilityId = fac?.FacilityId,
                AssignedFacilityName = fac?.FacilityName
            };
        }).ToList();
    }
}
