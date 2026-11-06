using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Models;
using FrontEnd.Services.Api;

namespace FrontEnd.Controllers;

public class AccountController : Controller
{
    private readonly TutorFlowApiClient _apiClient;

    public AccountController(TutorFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        model.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _apiClient.LoginAsync(model.Email, model.Password, model.RememberMe);
        if (!result.Success || result.Value == null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var user = result.Value.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.RoleName),
            new("access_token", result.Value.AccessToken)
        };

        if (user.FacilityId.HasValue)
        {
            claims.Add(new Claim("FacilityId", user.FacilityId.Value.ToString()));
            if (!string.IsNullOrEmpty(user.FacilityName))
            {
                claims.Add(new Claim("FacilityName", user.FacilityName));
            }
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = result.Value.ExpiresAtUtc
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToLocal(returnUrl, user.RoleName);
    }

    [HttpPost]
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl, string? roleName = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
        {
            return LocalRedirect(returnUrl);
        }

        return (roleName ?? User.FindFirst(ClaimTypes.Role)?.Value) switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "GrandManager" => RedirectToAction("Index", "Home"),
            "FacilityManager" => RedirectToAction("Index", "Home"),
            "Lecturer" => RedirectToAction("Index", "Home"),
            "Student" => RedirectToAction("Index", "Home"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
