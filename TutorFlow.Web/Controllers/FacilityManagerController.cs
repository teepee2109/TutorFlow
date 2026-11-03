using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorFlow.Services.Common;

namespace TutorFlow.Web.Controllers;

/// <summary>
/// Dashboard trang chủ cho Facility Manager — thống kê cơ sở phụ trách (F3, F6, F7, F10).
/// </summary>
[Authorize(Roles = "FacilityManager")]
public class FacilityManagerController : Controller
{
    private readonly ICurrentUser _currentUser;

    public FacilityManagerController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Facility Manager Dashboard";
        ViewData["FacilityId"] = _currentUser.FacilityId;
        return View();
    }
}
