using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Web.Models.ViewModels;

namespace Sycota.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly IClubService _clubService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        IClubService clubService,
        UserManager<ApplicationUser> userManager)
    {
        _clubService = clubService;
        _userManager = userManager;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return View(new NotificationsViewModel());
        }

        var invitationsResult = await _clubService.GetPendingInvitationsForUserAsync(user.Email);

        var viewModel = new NotificationsViewModel
        {
            PendingInvitations = invitationsResult.Success ? invitationsResult.Data : []
        };

        return View(viewModel);
    }

    // GET: /Notifications/GetCount (AJAX endpoint for notification badge)
    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return Json(new { count = 0 });
        }

        var invitationsResult = await _clubService.GetPendingInvitationsForUserAsync(user.Email);
        var count = invitationsResult.Success ? invitationsResult.Data.Count() : 0;

        return Json(new { count });
    }
}
