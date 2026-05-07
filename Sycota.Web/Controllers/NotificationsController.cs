using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Web.Models.ViewModels;
using Sycota.Web.Services;

namespace Sycota.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly IClubService _clubService;
    private readonly IGamificationNotificationService _gamificationNotificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        IClubService clubService,
        IGamificationNotificationService gamificationNotificationService,
        UserManager<ApplicationUser> userManager)
    {
        _clubService = clubService;
        _gamificationNotificationService = gamificationNotificationService;
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
        var gamificationNotifications = _gamificationNotificationService.GetNotifications(user.Id)
            .Select(n => new GamificationNotificationViewModel
            {
                Id = n.Id,
                ClubId = n.ClubId,
                ClubName = n.ClubName,
                BadgeTitle = n.BadgeTitle,
                BadgeDescription = n.BadgeDescription,
                UnlockedAtUtc = n.UnlockedAtUtc,
                IsRead = n.IsRead
            });

        var viewModel = new NotificationsViewModel
        {
            PendingInvitations = invitationsResult.Success ? invitationsResult.Data : [],
            GamificationNotifications = gamificationNotifications
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
        var invitationCount = invitationsResult.Success ? invitationsResult.Data.Count() : 0;
        var gamificationCount = _gamificationNotificationService.GetUnreadCount(user.Id);
        var count = invitationCount + gamificationCount;

        return Json(new { count });
    }

    // GET: /Notifications/Achievement?notificationId=x
    public async Task<IActionResult> Achievement(string notificationId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var notification = _gamificationNotificationService.MarkAsRead(user.Id, notificationId);
        if (notification == null)
        {
            TempData["Error"] = "Известието не беше намерено.";
            return RedirectToAction(nameof(Index));
        }

        var model = new GamificationNotificationViewModel
        {
            Id = notification.Id,
            ClubId = notification.ClubId,
            ClubName = notification.ClubName,
            BadgeTitle = notification.BadgeTitle,
            BadgeDescription = notification.BadgeDescription,
            UnlockedAtUtc = notification.UnlockedAtUtc,
            IsRead = true
        };

        return View("Achievement", model);
    }
}
