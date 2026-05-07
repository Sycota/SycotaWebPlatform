using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Web.Models.ViewModels;
using Sycota.Web.Services;

namespace Sycota.Web.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly IClubService _clubService;
    private readonly IGamificationNotificationService _gamificationNotificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBadgeViewComponent(
        IClubService clubService,
        IGamificationNotificationService gamificationNotificationService,
        UserManager<ApplicationUser> userManager)
    {
        _clubService = clubService;
        _gamificationNotificationService = gamificationNotificationService;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return View(new NotificationsViewModel());
        }

        var invitationsResult = await _clubService.GetPendingInvitationsForUserAsync(user.Email);
        var gamificationNotifications = _gamificationNotificationService.GetNotifications(user.Id, unreadOnly: true)
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
}
