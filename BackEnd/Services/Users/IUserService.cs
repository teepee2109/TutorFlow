using TutorFlow.Services.Common;

namespace TutorFlow.Services.Users;

public interface IUserService
{
    Task<Result<UserPagedResult>> GetUsersAsync(
        string? roleFilter = null,
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10);

    Task<Result<UserDto>> GetUserByIdAsync(int id);

    Task<Result<UserDto>> CreateUserAsync(UserCreateDto dto, string operatorRole);

    Task<Result<UserDto>> UpdateUserAsync(UserUpdateDto dto, string operatorRole);

    Task<Result> ToggleActiveAsync(int userId, bool isActive, string operatorRole);

    Task<Result> SoftDeleteAsync(int userId, string operatorRole);

    Task<Result> ResetPasswordAsync(int userId, string newPassword, string operatorRole);

    Task<Result> AssignFacilityManagerAsync(int facilityId, int? managerId, string operatorRole);

    Task<List<UserDto>> GetEligibleFacilityManagersAsync();
}
