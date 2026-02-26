using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Web.Models.ViewModels;

namespace Sycota.Web.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly IClubService _clubService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBadgeViewComponent(
        IClubService clubService,
        UserManager<ApplicationUser> userManager)
    {
        _clubService = clubService;
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

        var viewModel = new NotificationsViewModel
        {
            PendingInvitations = invitationsResult.Success ? invitationsResult.Data : []
        };

        return View(viewModel);
    }
}
