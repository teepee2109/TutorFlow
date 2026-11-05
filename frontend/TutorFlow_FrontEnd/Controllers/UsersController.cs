using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Common;
using TutorFlow.Services.Users;
using TutorFlow.Web.Models;

namespace TutorFlow.Web.Controllers;

[Authorize(Roles = "Admin,GrandManager")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;
    private readonly TutorFlowDbContext _db;

    public UsersController(IUserService userService, ICurrentUser currentUser, TutorFlowDbContext db)
    {
        _userService = userService;
        _currentUser = currentUser;
        _db = db;
    }

    // GET: /Users
    public async Task<IActionResult> Index(string? roleFilter, string? search, bool? isActive, int page = 1)
    {
        var result = await _userService.GetUsersAsync(roleFilter, search, isActive, page, 10);
        var viewModel = new UserListViewModel
        {
            UsersResult = result.Value ?? new UserPagedResult(),
            RoleFilter = roleFilter,
            Search = search,
            IsActive = isActive,
            CurrentPage = page
        };

        return View(viewModel);
    }

    // GET: /Users/Create
    public async Task<IActionResult> Create()
    {
        var model = new UserCreateViewModel
        {
            RolesSelectList = await GetRolesSelectListAsync()
        };
        return View(model);
    }

    // POST: /Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        var result = await _userService.CreateUserAsync(new UserCreateDto
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            RoleId = model.RoleId,
            Password = model.Password
        }, _currentUser.Role ?? "");

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET: /Users/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.Success || result.Value == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        var u = result.Value;
        var model = new UserEditViewModel
        {
            UserId = u.UserId,
            Email = u.Email,
            FullName = u.FullName,
            PhoneNumber = u.PhoneNumber,
            RoleId = u.RoleId,
            RolesSelectList = await GetRolesSelectListAsync()
        };

        return View(model);
    }

    // POST: /Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        var result = await _userService.UpdateUserAsync(new UserUpdateDto
        {
            UserId = model.UserId,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            RoleId = model.RoleId
        }, _currentUser.Role ?? "");

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ToggleActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool active, string? roleFilter, string? search, bool? isActive, int currentPage = 1)
    {
        var result = await _userService.ToggleActiveAsync(id, active, _currentUser.Role ?? "");
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, page = currentPage });
    }

    // POST: /Users/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? roleFilter, string? search, bool? isActive, int currentPage = 1)
    {
        var result = await _userService.SoftDeleteAsync(id, _currentUser.Role ?? "");
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, page = currentPage });
    }

    // POST: /Users/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword, string? roleFilter, string? search, bool? isActive, int currentPage = 1)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập mật khẩu mới.";
            return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, page = currentPage });
        }

        var result = await _userService.ResetPasswordAsync(id, newPassword, _currentUser.Role ?? "");
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, page = currentPage });
    }

    // GET: /Users/Facilities (Admin only)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Facilities()
    {
        var facilities = await _db.Facilities
            .Include(f => f.Manager)
            .Include(f => f.Rooms)
            .AsNoTracking()
            .ToListAsync();

        var model = new FacilitiesViewModel
        {
            FacilitiesList = facilities.Select(f => new FacilityItemViewModel
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                Address = f.Address,
                ManagerId = f.ManagerId,
                ManagerName = f.Manager?.FullName,
                ManagerEmail = f.Manager?.Email,
                RoomCount = f.Rooms.Count
            }).ToList(),
            EligibleFMs = await _userService.GetEligibleFacilityManagersAsync()
        };

        return View(model);
    }

    // POST: /Users/AssignFacility
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignFacility(int facilityId, int? managerId)
    {
        var result = await _userService.AssignFacilityManagerAsync(facilityId, managerId, _currentUser.Role ?? "");
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Facilities));
    }

    private async Task<SelectList> GetRolesSelectListAsync()
    {
        var rolesQuery = _db.Roles.AsNoTracking();
        if (_currentUser.Role == "GrandManager")
        {
            rolesQuery = rolesQuery.Where(r => r.RoleName != "Admin" && r.RoleName != "GrandManager");
        }

        var roles = await rolesQuery.ToListAsync();
        return new SelectList(roles, nameof(Role.RoleId), nameof(Role.RoleName));
    }
}
