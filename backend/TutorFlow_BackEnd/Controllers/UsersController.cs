using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorFlow.BackEnd.Dtos;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Common;
using TutorFlow.Services.Users;

namespace TutorFlow.BackEnd.Controllers;

[ApiController]
[Authorize(Roles = "Admin,GrandManager")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly TutorFlowDbContext _db;

    public UsersController(IUserService users, TutorFlowDbContext db)
    {
        _users = users;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserPagedResult>>> GetUsers(
        string? roleFilter, string? search, bool? isActive, int page = 1, int pageSize = 10)
    {
        var result = await _users.GetUsersAsync(roleFilter, search, isActive, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));
        return ToActionResult(result);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(role => role.RoleId)
            .Select(role => new RoleDto { RoleId = role.RoleId, RoleName = role.RoleName })
            .ToListAsync();
        if (User.IsInRole("GrandManager"))
        {
            roles = roles.Where(role => role.RoleName is not "Admin" and not "GrandManager").ToList();
        }

        return Ok(ApiResponse<List<RoleDto>>.Ok(roles));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id) => ToActionResult(await _users.GetUserByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(UserCreateDto dto) =>
        ToActionResult(await _users.CreateUserAsync(dto, CurrentRole));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, UserUpdateDto dto)
    {
        dto.UserId = id;
        return ToActionResult(await _users.UpdateUserAsync(dto, CurrentRole));
    }

    [HttpPut("{id:int}/active")]
    public async Task<ActionResult<ApiResponse<bool>>> SetActive(int id, SetActiveRequest request) =>
        ToActionResult(await _users.ToggleActiveAsync(id, request.IsActive, CurrentRole), true);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id) =>
        ToActionResult(await _users.SoftDeleteAsync(id, CurrentRole), true);

    [HttpPut("{id:int}/password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(int id, ResetPasswordRequest request) =>
        ToActionResult(await _users.ResetPasswordAsync(id, request.NewPassword, CurrentRole), true);

    [HttpGet("eligible-facility-managers")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetEligibleFacilityManagers() =>
        Ok(ApiResponse<List<UserDto>>.Ok(await _users.GetEligibleFacilityManagersAsync()));

    [HttpGet("facilities")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetFacilities()
    {
        var items = await _db.Facilities.AsNoTracking()
            .OrderBy(facility => facility.FacilityId)
            .Select(facility => new FacilityDto
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Address = facility.Address,
                ManagerId = facility.ManagerId,
                ManagerName = facility.Manager == null ? null : facility.Manager.FullName,
                ManagerEmail = facility.Manager == null ? null : facility.Manager.Email,
                RoomCount = facility.Rooms.Count
            })
            .ToListAsync();

        return Ok(ApiResponse<List<FacilityDto>>.Ok(items));
    }

    [HttpPut("facilities/{facilityId:int}/manager")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignFacilityManager(int facilityId, AssignFacilityManagerRequest request) =>
        ToActionResult(await _users.AssignFacilityManagerAsync(facilityId, request.ManagerId, CurrentRole), true);

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private ActionResult<ApiResponse<T>> ToActionResult<T>(Result<T> result)
    {
        if (result.Success && result.Value is not null)
        {
            return Ok(ApiResponse<T>.Ok(result.Value, result.Message));
        }

        var response = ApiResponse<T>.Fail(result.Message);
        return result.Error == ScheduleError.NotFound ? NotFound(response) : BadRequest(response);
    }

    private ActionResult<ApiResponse<T>> ToActionResult<T>(Result result, T successValue)
    {
        if (result.Success)
        {
            return Ok(ApiResponse<T>.Ok(successValue, result.Message));
        }

        var response = ApiResponse<T>.Fail(result.Message);
        return result.Error == ScheduleError.NotFound ? NotFound(response) : BadRequest(response);
    }
}
