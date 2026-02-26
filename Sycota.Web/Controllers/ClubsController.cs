using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Web.Models.ViewModels;

namespace Sycota.Web.Controllers;

[Authorize]
public class ClubsController : Controller
{
    private readonly IClubRepository _clubRepository;
    private readonly IClubMemberRepository _clubMemberRepository;
    private readonly IClubService _clubService;
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClubsController(
        IClubRepository clubRepository,
        IClubMemberRepository clubMemberRepository,
        IClubService clubService,
        ITrainingSessionRepository trainingSessionRepository,
        UserManager<ApplicationUser> userManager)
    {
        _clubRepository = clubRepository;
        _clubMemberRepository = clubMemberRepository;
        _clubService = clubService;
        _trainingSessionRepository = trainingSessionRepository;
        _userManager = userManager;
    }

    // GET: /Clubs
    public async Task<IActionResult> Index()
    {
        var clubs = await _clubRepository.GetAllClubsAsync(ClubIncludeOptions.Members);
        var userId = _userManager.GetUserId(User);

        ClubMember? currentMembership = null;
        if (!string.IsNullOrEmpty(userId))
        {
            var allMembers = await _clubMemberRepository.GetAllClubMembersAsync(ClubMemberIncludeOptions.Club);
            currentMembership = allMembers.FirstOrDefault(m => m.UserId == userId);
        }

        var viewModel = new ClubIndexViewModel
        {
            Clubs = clubs,
            CurrentMembership = currentMembership
        };

        return View(viewModel);
    }

    // GET: /Clubs/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var club = await _clubRepository.GetClubByIdAsync(id, ClubIncludeOptions.All);
        if (club == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        ClubMember? currentMembership = null;
        bool hasPendingRequest = false;
        int pendingRequestsCount = 0;

        if (!string.IsNullOrEmpty(userId))
        {
            currentMembership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
            
            var pendingResult = await _clubService.HasPendingJoinRequestAsync(userId, id);
            hasPendingRequest = pendingResult.Success && pendingResult.Data;

            if (currentMembership?.Role == ClubRole.Admin)
            {
                var requestsResult = await _clubService.GetPendingJoinRequestsAsync(id);
                pendingRequestsCount = requestsResult.Success ? requestsResult.Data.Count() : 0;
            }
        }

        var membersResult = await _clubService.GetClubMembersAsync(id, ClubMemberIncludeOptions.User);
        var trainersResult = await _clubService.GetTrainersAsync(id, ClubMemberIncludeOptions.User);
        var competitorsResult = await _clubService.GetCompetitorsAsync(id, ClubMemberIncludeOptions.All);

        var viewModel = new ClubDetailsViewModel
        {
            Club = club,
            CurrentMembership = currentMembership,
            Members = membersResult.Success ? membersResult.Data : [],
            Trainers = trainersResult.Success ? trainersResult.Data : [],
            Competitors = competitorsResult.Success ? competitorsResult.Data : [],
            HasPendingRequest = hasPendingRequest,
            PendingRequestsCount = pendingRequestsCount
        };

        return View(viewModel);
    }

    // GET: /Clubs/Create
    public IActionResult Create()
    {
        return View(new CreateClubViewModel());
    }

    // POST: /Clubs/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClubViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var club = new Club
        {
            Name = model.Name,
            Description = model.Description,
            Address = model.Address,
            ContactEmail = model.ContactEmail,
            ContactPhone = model.ContactPhone,
            RequiresApproval = model.RequiresApproval,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _clubRepository.AddClubAsync(club);

        // Automatically add the creator as an admin
        await _clubService.AddClubMemberAsync(userId, club.Id, ClubRole.Admin);

        return RedirectToAction(nameof(Details), new { id = club.Id });
    }

    // GET: /Clubs/Join/5
    public async Task<IActionResult> Join(int id)
    {
        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(userId))
        {
            var existingMembership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
            if (existingMembership != null)
            {
                TempData["Error"] = "You are already a member of this club.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var pendingResult = await _clubService.HasPendingJoinRequestAsync(userId, id);
            if (pendingResult.Success && pendingResult.Data)
            {
                TempData["Error"] = "You already have a pending request to join this club.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        var trainersResult = await _clubService.GetTrainersAsync(id, ClubMemberIncludeOptions.User);

        var viewModel = new JoinClubViewModel
        {
            ClubId = id,
            AvailableTrainers = trainersResult.Success ? trainersResult.Data : []
        };

        ViewBag.ClubName = club.Name;
        ViewBag.RequiresApproval = club.RequiresApproval;
        return View(viewModel);
    }

    // POST: /Clubs/Join
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinClubViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var club = await _clubRepository.GetClubByIdAsync(model.ClubId);
        if (club == null)
        {
            return NotFound();
        }

        if (club.RequiresApproval)
        {
            // Create a join request instead of directly joining
            var result = await _clubService.CreateJoinRequestAsync(userId, model.ClubId, model.RequestedRole, model.TrainerId, model.Message);
            
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Join), new { id = model.ClubId });
            }

            TempData["Success"] = "Your request to join the club has been submitted. An admin will review it shortly.";
        }
        else
        {
            // Direct join
            var result = await _clubService.AddClubMemberAsync(userId, model.ClubId, model.RequestedRole, model.TrainerId);
            
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Join), new { id = model.ClubId });
            }

            TempData["Success"] = "You have successfully joined the club!";
        }

        return RedirectToAction(nameof(Details), new { id = model.ClubId });
    }

    // GET: /Clubs/ManageRequests/5
    public async Task<IActionResult> ManageRequests(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.Club);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can manage join requests.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var requestsResult = await _clubService.GetPendingJoinRequestsAsync(id);
        var invitationsResult = await _clubService.GetPendingInvitationsAsync(id);

        var viewModel = new ManageRequestsViewModel
        {
            Club = club,
            PendingRequests = requestsResult.Success ? requestsResult.Data : [],
            PendingInvitations = invitationsResult.Success ? invitationsResult.Data : []
        };

        return View(viewModel);
    }

    // POST: /Clubs/ApproveRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id, int requestId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can approve requests.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _clubService.ApproveJoinRequestAsync(requestId, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Request approved successfully.";
        }

        return RedirectToAction(nameof(ManageRequests), new { id });
    }

    // POST: /Clubs/RejectRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, int requestId, string? reason)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can reject requests.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _clubService.RejectJoinRequestAsync(requestId, userId, reason);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Request rejected.";
        }

        return RedirectToAction(nameof(ManageRequests), new { id });
    }

    // GET: /Clubs/Invite/5
    public async Task<IActionResult> Invite(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can invite members.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var trainersResult = await _clubService.GetTrainersAsync(id, ClubMemberIncludeOptions.User);

        var viewModel = new CreateInvitationViewModel
        {
            ClubId = id,
            AvailableTrainers = trainersResult.Success ? trainersResult.Data : []
        };

        ViewBag.ClubName = club.Name;
        return View(viewModel);
    }

    // POST: /Clubs/Invite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(CreateInvitationViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can invite members.";
            return RedirectToAction(nameof(Details), new { id = model.ClubId });
        }

        var result = await _clubService.CreateInvitationAsync(
            model.ClubId, 
            model.Email, 
            model.OfferedRole, 
            userId, 
            model.TrainerId, 
            model.Message, 
            model.ExpirationDays);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Invite), new { id = model.ClubId });
        }

        TempData["Success"] = $"Invitation sent to {model.Email}";
        return RedirectToAction(nameof(ManageRequests), new { id = model.ClubId });
    }

    // GET: /Clubs/AcceptInvitation?code=xxx
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(string code)
    {
        var invitationResult = await _clubService.GetInvitationByCodeAsync(code);
        if (!invitationResult.Success)
        {
            TempData["Error"] = invitationResult.Error;
            return RedirectToAction(nameof(Index));
        }

        var invitation = invitationResult.Data;

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            TempData["Error"] = "This invitation has expired.";
            return RedirectToAction(nameof(Index));
        }

        if (invitation.Status != MembershipRequestStatus.Pending)
        {
            TempData["Error"] = "This invitation has already been used or cancelled.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new AcceptInvitationViewModel
        {
            Invitation = invitation
        };

        return View(viewModel);
    }

    // POST: /Clubs/AcceptInvitation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvitation(string code, bool confirm)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ReturnUrl"] = Url.Action(nameof(AcceptInvitation), new { code });
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        var result = await _clubService.AcceptInvitationAsync(code, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(AcceptInvitation), new { code });
        }

        var invitationResult = await _clubService.GetInvitationByCodeAsync(code);
        TempData["Success"] = "You have successfully joined the club!";
        
        return RedirectToAction(nameof(Details), new { id = invitationResult.Data.ClubId });
    }

    // POST: /Clubs/CancelInvitation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelInvitation(int id, int invitationId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can cancel invitations.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _clubService.CancelInvitationAsync(invitationId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Invitation cancelled.";
        }

        return RedirectToAction(nameof(ManageRequests), new { id });
    }

    // GET: /Clubs/MyResults/5
    public async Task<IActionResult> MyResults(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            TempData["Error"] = "You are not a member of this club.";
            return RedirectToAction(nameof(Index));
        }

        var trainingSessions = await _trainingSessionRepository.GetAllTrainingSessionsByClubIdAsync(id, TrainingSessionIncludeOptions.All);
        var myTrainingSessions = trainingSessions.Where(ts => ts.CreatedById == userId).OrderByDescending(ts => ts.SessionDate);

        var viewModel = new MyResultsViewModel
        {
            CurrentMembership = membership,
            TrainingSessions = myTrainingSessions,
            ShooterProfile = membership.ShooterProfile
        };

        return View(viewModel);
    }

    // GET: /Clubs/Trainees/5
    public async Task<IActionResult> Trainees(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            TempData["Error"] = "You are not a member of this club.";
            return RedirectToAction(nameof(Index));
        }

        if (!membership.CanTrain)
        {
            TempData["Error"] = "Only trainers can view trainees.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var traineesResult = await _clubService.GetClubMembersForTrainerAsync(membership.Id, ClubMemberIncludeOptions.All);

        var viewModel = new TraineesViewModel
        {
            TrainerMembership = membership,
            Trainees = traineesResult.Success ? traineesResult.Data : []
        };

        return View(viewModel);
    }

    // GET: /Clubs/TraineeDetails/5?traineeId=10
    public async Task<IActionResult> TraineeDetails(int id, int traineeId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
        if (membership == null || !membership.CanTrain)
        {
            TempData["Error"] = "You must be a trainer to view trainee details.";
            return RedirectToAction(nameof(Index));
        }

        var traineeResult = await _clubService.GetClubMemberAsync(traineeId, ClubMemberIncludeOptions.All);
        if (!traineeResult.Success)
        {
            TempData["Error"] = traineeResult.Error;
            return RedirectToAction(nameof(Trainees), new { id });
        }

        var trainee = traineeResult.Data;
        if (trainee.TrainerId != membership.Id)
        {
            TempData["Error"] = "This competitor is not assigned to you.";
            return RedirectToAction(nameof(Trainees), new { id });
        }

        var trainingSessions = await _trainingSessionRepository.GetAllTrainingSessionsByClubIdAsync(id, TrainingSessionIncludeOptions.All);
        var traineeTrainingSessions = trainingSessions
            .Where(ts => ts.CreatedById == trainee.UserId)
            .OrderByDescending(ts => ts.SessionDate);

        var viewModel = new TraineeDetailsViewModel
        {
            Trainee = trainee,
            TrainingSessions = traineeTrainingSessions,
            ShooterProfile = trainee.ShooterProfile
        };

        return View(viewModel);
    }

    // POST: /Clubs/Leave/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null)
        {
            TempData["Error"] = "You are not a member of this club.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _clubService.RemoveClubMemberAsync(membership.Id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "You have left the club.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Clubs/ManageMembers/5
    public async Task<IActionResult> ManageMembers(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can manage members.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var trainersResult = await _clubService.GetTrainersAsync(id, ClubMemberIncludeOptions.All);
        var competitorsResult = await _clubService.GetCompetitorsAsync(id, ClubMemberIncludeOptions.All);
        var adminsResult = await _clubService.GetAdminsAsync(id, ClubMemberIncludeOptions.User);

        var viewModel = new ManageMembersViewModel
        {
            Club = club,
            Trainers = trainersResult.Success ? trainersResult.Data : [],
            Competitors = competitorsResult.Success ? competitorsResult.Data : [],
            Admins = adminsResult.Success ? adminsResult.Data : []
        };

        return View(viewModel);
    }

    // GET: /Clubs/AssignTrainer/5?competitorId=10
    public async Task<IActionResult> AssignTrainer(int id, int competitorId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can assign trainers.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var competitorResult = await _clubService.GetClubMemberAsync(competitorId, ClubMemberIncludeOptions.All);
        if (!competitorResult.Success)
        {
            TempData["Error"] = competitorResult.Error;
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var competitor = competitorResult.Data;
        if (competitor.ClubId != id || competitor.Role != ClubRole.Competitor)
        {
            TempData["Error"] = "Invalid competitor selected.";
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var trainersResult = await _clubService.GetTrainersAsync(id, ClubMemberIncludeOptions.User);

        var competitorName = !string.IsNullOrEmpty(competitor.User?.FirstName) 
            ? $"{competitor.User.FirstName} {competitor.User.LastName}" 
            : competitor.User?.UserName ?? "Unknown";

        var currentTrainerName = competitor.Trainer != null 
            ? (!string.IsNullOrEmpty(competitor.Trainer.User?.FirstName) 
                ? $"{competitor.Trainer.User.FirstName} {competitor.Trainer.User.LastName}" 
                : competitor.Trainer.User?.UserName)
            : null;

        var viewModel = new AssignTrainerViewModel
        {
            ClubId = id,
            CompetitorId = competitorId,
            CompetitorName = competitorName,
            CurrentTrainerId = competitor.TrainerId,
            CurrentTrainerName = currentTrainerName,
            SelectedTrainerId = competitor.TrainerId,
            AvailableTrainers = trainersResult.Success ? trainersResult.Data : []
        };

        return View(viewModel);
    }

    // POST: /Clubs/AssignTrainer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTrainer(AssignTrainerViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can assign trainers.";
            return RedirectToAction(nameof(Details), new { id = model.ClubId });
        }

        var result = await _clubService.AssignTrainerToCompetitorAsync(model.CompetitorId, model.SelectedTrainerId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(AssignTrainer), new { id = model.ClubId, competitorId = model.CompetitorId });
        }

        if (model.SelectedTrainerId.HasValue)
        {
            TempData["Success"] = "Trainer assigned successfully.";
        }
        else
        {
            TempData["Success"] = "Trainer assignment removed.";
        }

        return RedirectToAction(nameof(ManageMembers), new { id = model.ClubId });
    }

    // POST: /Clubs/RemoveMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int memberId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can remove members.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (membership.Id == memberId)
        {
            TempData["Error"] = "You cannot remove yourself. Use the Leave Club option instead.";
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var result = await _clubService.RemoveClubMemberAsync(memberId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Member removed from the club.";
        }

        return RedirectToAction(nameof(ManageMembers), new { id });
    }

    // POST: /Clubs/ToggleAdminTrainer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdminTrainer(int id, int memberId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can modify trainer status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var targetMemberResult = await _clubService.GetClubMemberAsync(memberId);
        if (!targetMemberResult.Success)
        {
            TempData["Error"] = targetMemberResult.Error;
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var targetMember = targetMemberResult.Data;
        if (targetMember.Role != ClubRole.Admin || targetMember.ClubId != id)
        {
            TempData["Error"] = "Can only toggle trainer status for admins in this club.";
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var result = await _clubService.SetAdminAsTrainerAsync(memberId, !targetMember.IsAlsoTrainer);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = targetMember.IsAlsoTrainer 
                ? "Trainer role disabled for this admin." 
                : "Trainer role enabled for this admin.";
        }

        return RedirectToAction(nameof(ManageMembers), new { id });
    }
}
