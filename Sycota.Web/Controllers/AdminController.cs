using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Infrastructure.Data;
using Sycota.Web.Models.ViewModels;

namespace Sycota.Web.Controllers;

[Authorize]
public class AdminController : Controller
{
    /*
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClubRepository _clubRepository;
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IClubMemberRepository _clubMemberRepository;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IClubRepository clubRepository,
        ITrainingSessionRepository trainingSessionRepository,
        IClubMemberRepository clubMemberRepository)
    {
        _context = context;
        _userManager = userManager;
        _clubRepository = clubRepository;
        _trainingSessionRepository = trainingSessionRepository;
        _clubMemberRepository = clubMemberRepository;
    }

    private async Task<bool> IsCurrentUserAdmin()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.IsAdmin == true;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var users = await _context.Users.ToListAsync();
        var clubs = await _clubRepository.GetAllClubsAsync(ClubIncludeOptions.Members);
        var sessions = await _trainingSessionRepository.GetAllTrainingSessionsAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalUsers = users.Count,
            TotalClubs = clubs.Count(),
            TotalSessions = sessions.Count(),
            TotalMembers = clubs.Sum(c => c.Members.Count),
            RecentUsers = users.OrderByDescending(u => u.Id).Take(5),
            RecentClubs = clubs.OrderByDescending(c => c.CreatedAt).Take(5)
        };

        return View(viewModel);
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users(string? search)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u =>
                u.UserName!.ToLower().Contains(term) ||
                u.Email!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var users = await query.OrderBy(u => u.UserName).ToListAsync();

        var viewModel = new AdminUsersViewModel
        {
            Users = users,
            SearchTerm = search
        };

        return View(viewModel);
    }

    // GET: /Admin/EditUser/id
    public async Task<IActionResult> EditUser(string id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new AdminEditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DOB = user.DOB,
            Nationality = user.Nationality,
            IsAdmin = user.IsAdmin,
            EmailConfirmed = user.EmailConfirmed
        };

        return View(viewModel);
    }

    // POST: /Admin/EditUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminEditUserViewModel model)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.DOB = model.DOB;
        user.Nationality = model.Nationality;
        user.IsAdmin = model.IsAdmin;
        user.EmailConfirmed = model.EmailConfirmed;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        TempData["Success"] = $"?????????? \"{user.UserName}\" ? ??????? ???????.";
        return RedirectToAction(nameof(Users));
    }

    // POST: /Admin/DeleteUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id)
        {
            TempData["Error"] = "?? ?????? ?? ???????? ?????????? ?? ??????.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "?????? ??? ????????? ?? ???????????.";
            return RedirectToAction(nameof(Users));
        }

        TempData["Success"] = $"?????????? \"{user.UserName}\" ? ??????.";
        return RedirectToAction(nameof(Users));
    }

    // GET: /Admin/Clubs
    public async Task<IActionResult> Clubs(string? search)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var clubs = await _clubRepository.GetAllClubsAsync(ClubIncludeOptions.All);
        var clubList = clubs.ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            clubList = clubList.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Description.ToLower().Contains(term) ||
                (c.ContactEmail?.ToLower().Contains(term) ?? false)).ToList();
        }

        var viewModel = new AdminClubsViewModel
        {
            Clubs = clubList.OrderByDescending(c => c.CreatedAt),
            SearchTerm = search
        };

        return View(viewModel);
    }

    // GET: /Admin/EditClub/5
    public async Task<IActionResult> EditClub(int id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var club = await _clubRepository.GetClubByIdAsync(id, ClubIncludeOptions.All);
        if (club == null)
        {
            return NotFound();
        }

        var creatorName = club.CreatedBy != null
            ? (!string.IsNullOrEmpty(club.CreatedBy.FirstName)
                ? $"{club.CreatedBy.FirstName} {club.CreatedBy.LastName}"
                : club.CreatedBy.UserName)
            : "??????????";

        var viewModel = new AdminEditClubViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            Address = club.Address,
            ContactEmail = club.ContactEmail,
            ContactPhone = club.ContactPhone,
            RequiresApproval = club.RequiresApproval,
            CreatedByName = creatorName,
            CreatedAt = club.CreatedAt,
            MemberCount = club.Members.Count
        };

        return View(viewModel);
    }

    // POST: /Admin/EditClub
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClub(AdminEditClubViewModel model)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var club = await _clubRepository.GetClubByIdAsync(model.Id);
        if (club == null)
        {
            return NotFound();
        }

        club.Name = model.Name;
        club.Description = model.Description;
        club.Address = model.Address;
        club.ContactEmail = model.ContactEmail;
        club.ContactPhone = model.ContactPhone;
        club.RequiresApproval = model.RequiresApproval;

        await _clubRepository.UpdateClubAsync(club);

        TempData["Success"] = $"???? \"{club.Name}\" ? ??????? ???????.";
        return RedirectToAction(nameof(Clubs));
    }

    // POST: /Admin/DeleteClub
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClub(int id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var clubName = club.Name;
        await _clubRepository.DeleteClubAsync(club);

        TempData["Success"] = $"???? \"{clubName}\" ? ??????.";
        return RedirectToAction(nameof(Clubs));
    }

    // GET: /Admin/Sessions
    public async Task<IActionResult> Sessions(string? search)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var sessions = await _trainingSessionRepository.GetAllTrainingSessionsAsync(TrainingSessionIncludeOptions.All);
        var sessionList = sessions.ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            sessionList = sessionList.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.Description?.ToLower().Contains(term) ?? false) ||
                (s.Club?.Name.ToLower().Contains(term) ?? false) ||
                (s.CreatedBy?.UserName?.ToLower().Contains(term) ?? false)).ToList();
        }

        var viewModel = new AdminSessionsViewModel
        {
            Sessions = sessionList.OrderByDescending(s => s.SessionDate),
            SearchTerm = search
        };

        return View(viewModel);
    }

    // GET: /Admin/EditSession/5
    public async Task<IActionResult> EditSession(int id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(id, TrainingSessionIncludeOptions.All);
        if (session == null)
        {
            return NotFound();
        }

        var creatorName = session.CreatedBy != null
            ? (!string.IsNullOrEmpty(session.CreatedBy.FirstName)
                ? $"{session.CreatedBy.FirstName} {session.CreatedBy.LastName}"
                : session.CreatedBy.UserName)
            : "??????????";

        var viewModel = new AdminEditSessionViewModel
        {
            Id = session.Id,
            Name = session.Name,
            Description = session.Description,
            SessionDate = session.SessionDate,
            WeaponType = session.WeaponType,
            Notes = session.Notes,
            ClubId = session.ClubId,
            ClubName = session.Club?.Name,
            CreatedByName = creatorName
        };

        return View(viewModel);
    }

    // POST: /Admin/EditSession
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSession(AdminEditSessionViewModel model)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(model.Id);
        if (session == null)
        {
            return NotFound();
        }

        session.Name = model.Name;
        session.Description = model.Description;
        session.SessionDate = model.SessionDate;
        session.WeaponType = model.WeaponType;
        session.Notes = model.Notes;

        await _trainingSessionRepository.UpdateTrainingSessionAsync(session);

        TempData["Success"] = $"????? \"{session.Name}\" ? ???????? ???????.";
        return RedirectToAction(nameof(Sessions));
    }

    // POST: /Admin/DeleteSession
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSession(int id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        var sessionName = session.Name;
        await _trainingSessionRepository.DeleteTrainingSessionAsync(session);

        TempData["Success"] = $"????? \"{sessionName}\" ? ???????.";
        return RedirectToAction(nameof(Sessions));
    }

    // GET: /Admin/Members
    public async Task<IActionResult> Members(string? search)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var members = await _clubMemberRepository.GetAllClubMembersAsync(ClubMemberIncludeOptions.All);
        var memberList = members.ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            memberList = memberList.Where(m =>
                (m.User?.UserName?.ToLower().Contains(term) ?? false) ||
                (m.User?.FirstName.ToLower().Contains(term) ?? false) ||
                (m.User?.LastName.ToLower().Contains(term) ?? false) ||
                (m.Club?.Name.ToLower().Contains(term) ?? false)).ToList();
        }

        var viewModel = new AdminMembersViewModel
        {
            Members = memberList.OrderBy(m => m.Club?.Name).ThenBy(m => m.User?.UserName),
            SearchTerm = search
        };

        return View(viewModel);
    }

    // POST: /Admin/DeleteMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMember(int id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var member = await _clubMemberRepository.GetClubMemberByIdAsync(id, ClubMemberIncludeOptions.User);
        if (member == null)
        {
            return NotFound();
        }

        var memberName = member.User?.UserName ?? "??????????";
        await _clubMemberRepository.DeleteClubMemberAsync(member);

        TempData["Success"] = $"?????????? ?? \"{memberName}\" ? ??????????.";
        return RedirectToAction(nameof(Members));
    }
    */
}
