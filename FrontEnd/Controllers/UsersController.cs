using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TutorFlow.Web.Models;
using TutorFlow.Web.Services.Api;

namespace TutorFlow.Web.Controllers;

[Authorize(Roles = "Admin,GrandManager")]
public class UsersController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public UsersController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(string? roleFilter, string? search, bool? isActive, int? facilityId, int page = 1)
    {
        var result = await _apiClient.GetUsersAsync(roleFilter, search, isActive, page, 10, facilityId);
        var facilityOptions = await _apiClient.GetFacilityOptionsAsync();
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
        }

        return View(new UserListViewModel
        {
            UsersResult = result.Value ?? new UserPagedResult(),
            RoleFilter = roleFilter,
            Search = search,
            IsActive = isActive,
            FacilityId = facilityId,
            CurrentPage = page,
            FacilityOptions = (facilityOptions.Value ?? new List<FacilityOptionViewModel>())
                .Select(f => new SelectListItem(f.FacilityName, f.FacilityId.ToString(), f.FacilityId == facilityId))
                .ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new UserCreateViewModel
        {
            RolesSelectList = await GetRolesSelectListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        var result = await _apiClient.CreateUserAsync(new UserCreateDto
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            RoleId = model.RoleId,
            Password = model.Password
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _apiClient.GetUserAsync(id);
        if (!result.Success || result.Value == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(new UserEditViewModel
        {
            UserId = result.Value.UserId,
            Email = result.Value.Email,
            FullName = result.Value.FullName,
            PhoneNumber = result.Value.PhoneNumber,
            RoleId = result.Value.RoleId,
            RolesSelectList = await GetRolesSelectListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        var result = await _apiClient.UpdateUserAsync(model.UserId, new UserUpdateDto
        {
            UserId = model.UserId,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            RoleId = model.RoleId
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.RolesSelectList = await GetRolesSelectListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool active, string? roleFilter, string? search, bool? isActive, int? facilityId, int currentPage = 1)
    {
        var result = await _apiClient.SetUserActiveAsync(id, active);
        SetOperationMessage(result);
        return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, facilityId, page = currentPage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? roleFilter, string? search, bool? isActive, int? facilityId, int currentPage = 1)
    {
        var result = await _apiClient.DeleteUserAsync(id);
        SetOperationMessage(result);
        return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, facilityId, page = currentPage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword, string? roleFilter, string? search, bool? isActive, int? facilityId, int currentPage = 1)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập mật khẩu mới.";
        }
        else
        {
            SetOperationMessage(await _apiClient.ResetPasswordAsync(id, newPassword));
        }

        return RedirectToAction(nameof(Index), new { roleFilter, search, isActive, facilityId, page = currentPage });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Facilities()
    {
        var result = await _apiClient.GetFacilitiesAsync();
        if (!result.Success || result.Value == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(new FacilitiesViewModel());
        }

        return View(new FacilitiesViewModel
        {
            FacilitiesList = result.Value.Facilities,
            EligibleFMs = result.Value.EligibleManagers
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignFacility(int facilityId, int? managerId)
    {
        SetOperationMessage(await _apiClient.AssignFacilityManagerAsync(facilityId, managerId));
        return RedirectToAction(nameof(Facilities));
    }

    private async Task<SelectList> GetRolesSelectListAsync()
    {
        var result = await _apiClient.GetRolesAsync();
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
        }

        var roles = result.Value ?? new List<RoleOptionDto>();
        return new SelectList(roles, nameof(RoleOptionDto.RoleId), nameof(RoleOptionDto.RoleName));
    }

    private void SetOperationMessage(ApiResponse result)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
    }
}
