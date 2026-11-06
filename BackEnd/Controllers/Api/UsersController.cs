using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using BackEnd.Dtos.Api;
using TutorFlow.Services.Users;

namespace BackEnd.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,GrandManager")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly TutorFlowDbContext _db;

    public UsersController(IUserService userService, TutorFlowDbContext db)
    {
        _userService = userService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? roleFilter,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetUsersAsync(roleFilter, search, isActive, page, pageSize);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<UserPagedResult>.Ok(result.Value))
            : BadRequest(ApiResponse<UserPagedResult>.Fail(result.Message));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var operatorRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var query = _db.Roles.AsNoTracking();
        if (operatorRole == "GrandManager")
        {
            query = query.Where(role => role.RoleName != "Admin" && role.RoleName != "GrandManager");
        }

        var roles = await query.OrderBy(role => role.RoleId)
            .Select(role => new RoleOptionDto { RoleId = role.RoleId, RoleName = role.RoleName })
            .ToListAsync();
        return Ok(ApiResponse<List<RoleOptionDto>>.Ok(roles));
    }

    [HttpGet("facilities")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFacilities()
    {
        var facilities = await _db.Facilities
            .Include(facility => facility.Manager)
            .Include(facility => facility.Rooms)
            .AsNoTracking()
            .OrderBy(facility => facility.FacilityId)
            .Select(facility => new FacilityItemDto
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

        var data = new FacilitiesDataDto
        {
            Facilities = facilities,
            EligibleManagers = await _userService.GetEligibleFacilityManagersAsync()
        };
        return Ok(ApiResponse<FacilitiesDataDto>.Ok(data));
    }

    [HttpPut("facilities/{facilityId:int}/manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignFacilityManager(int facilityId, AssignFacilityManagerRequest request)
    {
        var result = await _userService.AssignFacilityManagerAsync(facilityId, request.ManagerId, CurrentRole);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Message))
            : BadRequest(ApiResponse.Fail(result.Message));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<UserDto>.Ok(result.Value))
            : NotFound(ApiResponse<UserDto>.Fail(result.Message));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(UserCreateDto request)
    {
        var result = await _userService.CreateUserAsync(request, CurrentRole);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<UserDto>.Ok(result.Value, result.Message))
            : BadRequest(ApiResponse<UserDto>.Fail(result.Message));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UserUpdateDto request)
    {
        request.UserId = id;
        var result = await _userService.UpdateUserAsync(request, CurrentRole);
        return result.Success && result.Value != null
            ? Ok(ApiResponse<UserDto>.Ok(result.Value, result.Message))
            : BadRequest(ApiResponse<UserDto>.Fail(result.Message));
    }

    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, UserActiveRequest request)
    {
        var result = await _userService.ToggleActiveAsync(id, request.IsActive, CurrentRole);
        return result.Success ? Ok(ApiResponse.Ok(result.Message)) : BadRequest(ApiResponse.Fail(result.Message));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.SoftDeleteAsync(id, CurrentRole);
        return result.Success ? Ok(ApiResponse.Ok(result.Message)) : BadRequest(ApiResponse.Fail(result.Message));
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
    {
        var result = await _userService.ResetPasswordAsync(id, request.NewPassword, CurrentRole);
        return result.Success ? Ok(ApiResponse.Ok(result.Message)) : BadRequest(ApiResponse.Fail(result.Message));
    }

    private string CurrentRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
}
