using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Web.ViewComponents;

public class ClubsMenuViewComponent : ViewComponent
{
    private readonly IClubMemberRepository _clubMemberRepository;
    private readonly IClubRepository _clubRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClubsMenuViewComponent(
        IClubMemberRepository clubMemberRepository,
        IClubRepository clubRepository,
        UserManager<ApplicationUser> userManager)
    {
        _clubMemberRepository = clubMemberRepository;
        _clubRepository = clubRepository;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(HttpContext.User);
        if (string.IsNullOrEmpty(userId))
        {
            return View(new List<Club>());
        }

        var memberships = (await _clubMemberRepository.GetAllClubMembersAsync())
            .Where(m => m.UserId == userId)
            .ToList();

        var myClubIds = memberships.Select(m => m.ClubId).Distinct().ToHashSet();

        var clubs = (await _clubRepository.GetAllClubsAsync(ClubIncludeOptions.None))
            .Where(c => myClubIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToList();

        return View(clubs);
    }
}
