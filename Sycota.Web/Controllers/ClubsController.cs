using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Web.Models.ViewModels;
using System.Text.Json;

namespace Sycota.Web.Controllers;

[Authorize]
public class ClubsController : Controller
{
    private readonly IClubRepository _clubRepository;
    private readonly IClubMemberRepository _clubMemberRepository;
    private readonly IClubService _clubService;
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    // Constants for ISSF 10m Air Rifle scoring
    private const double PELLET_RADIUS = 2.25;   // 4.5mm pellet diameter / 2
    private const double RING_10_BOUNDARY = 0.25; // Inner edge boundary for a 10 score

    // Ring outer radii for rings 9 down to 1
    private static readonly double[] RingRadii = { 2.5, 5.0, 7.5, 10.0, 12.5, 15.0, 17.5, 20.0, 22.5 };

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
            ShooterProfile = trainee.ShooterProfile,
            TrainerMembership = membership
        };

        return View(viewModel);
    }

    // GET: /Clubs/TraineePerformanceDashboard/5?traineeId=10
    public async Task<IActionResult> TraineePerformanceDashboard(int id, int traineeId, int days = 30)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
        if (membership == null || !membership.CanTrain)
        {
            TempData["Error"] = "You must be a trainer to view trainee performance.";
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

        var allSessions = await _trainingSessionRepository.GetAllTrainingSessionsByClubIdAsync(id, TrainingSessionIncludeOptions.All);
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var traineeSessions = allSessions
            .Where(ts => ts.CreatedById == trainee.UserId && ts.SessionDate >= cutoffDate)
            .OrderBy(ts => ts.SessionDate)
            .ToList();

        var statistics = CalculatePerformanceStatistics(traineeSessions);
        var chartData = GenerateChartData(traineeSessions, statistics);
        var heatMapData = GenerateHeatMapData(traineeSessions);

        var viewModel = new TraineePerformanceDashboardViewModel
        {
            TrainerMembership = membership,
            Trainee = trainee,
            TrainingSessions = traineeSessions,
            Statistics = statistics,
            ChartDataJson = JsonSerializer.Serialize(chartData),
            HeatMapDataJson = JsonSerializer.Serialize(heatMapData),
            SelectedDays = days
        };

        return View(viewModel);
    }

    // GET: /Clubs/TraineeSessionDetails/5?traineeId=10&sessionId=20
    public async Task<IActionResult> TraineeSessionDetails(int id, int traineeId, int sessionId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
        if (membership == null || !membership.CanTrain)
        {
            TempData["Error"] = "You must be a trainer to view trainee sessions.";
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

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(sessionId, TrainingSessionIncludeOptions.All);
        if (session == null || session.CreatedById != trainee.UserId)
        {
            TempData["Error"] = "Session not found or does not belong to this trainee.";
            return RedirectToAction(nameof(TraineeDetails), new { id, traineeId });
        }

        var viewModel = new TraineeSessionDetailsViewModel
        {
            Session = session,
            TrainerMembership = membership,
            Trainee = trainee
        };

        return View(viewModel);
    }

    // GET: /Clubs/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can edit club details.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var viewModel = new EditClubViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            Address = club.Address,
            ContactEmail = club.ContactEmail,
            ContactPhone = club.ContactPhone,
            RequiresApproval = club.RequiresApproval
        };

        return View(viewModel);
    }

    // POST: /Clubs/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditClubViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.Id);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Only admins can edit club details.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
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

        TempData["Success"] = "Club details updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
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

    // GET: /Clubs/PerformanceDashboard/5
    public async Task<IActionResult> PerformanceDashboard(int id, int days = 30)
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

        var allSessions = await _trainingSessionRepository.GetAllTrainingSessionsByClubIdAsync(id, TrainingSessionIncludeOptions.All);
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var myTrainingSessions = allSessions
            .Where(ts => ts.CreatedById == userId && ts.SessionDate >= cutoffDate)
            .OrderBy(ts => ts.SessionDate)
            .ToList();

        var statistics = CalculatePerformanceStatistics(myTrainingSessions);
        var chartData = GenerateChartData(myTrainingSessions, statistics);
        var heatMapData = GenerateHeatMapData(myTrainingSessions);

        var viewModel = new PerformanceDashboardViewModel
        {
            CurrentMembership = membership,
            TrainingSessions = myTrainingSessions,
            Statistics = statistics,
            ChartDataJson = JsonSerializer.Serialize(chartData),
            HeatMapDataJson = JsonSerializer.Serialize(heatMapData),
            SelectedDays = days
        };

        return View(viewModel);
    }

    private PerformanceStatistics CalculatePerformanceStatistics(List<TrainingSession> sessions)
    {
        var statistics = new PerformanceStatistics
        {
            TotalSessions = sessions.Count
        };

        if (!sessions.Any())
        {
            return statistics;
        }

        var allSeriesPerformance = new List<SeriesPerformance>();
        var dailyStats = new Dictionary<DateTime, (double totalScore, int totalShots, int sessionCount)>();

        foreach (var session in sessions)
        {
            var shotsData = ParseShotsData(session.Shots);
            if (shotsData == null) continue;

            var sessionDate = session.SessionDate.Date;

            // Process each group/series
            for (int i = 0; i < shotsData.Groups.Count; i++)
            {
                var group = shotsData.Groups[i];
                if (group.ValueType != "10-shot-series" || !group.Shots.Any()) continue;

                double seriesScore = 0;
                int tens = 0;
                int innerTens = 0;

                foreach (var shot in group.Shots)
                {
                    var score = CalculateShotScore(shot.X, shot.Y);
                    seriesScore += score;
                    statistics.TotalShots++;

                    if (score >= 10.0)
                    {
                        tens++;
                        if (score >= 10.5)
                        {
                            innerTens++;
                        }
                    }
                }

                statistics.TotalTens += tens;
                statistics.TotalInnerTens += innerTens;

                var seriesPerf = new SeriesPerformance
                {
                    Date = session.SessionDate,
                    SessionName = session.Name,
                    SeriesNumber = i + 1,
                    Score = seriesScore,
                    ShotCount = group.Shots.Count,
                    AveragePerShot = group.Shots.Count > 0 ? seriesScore / group.Shots.Count : 0,
                    Tens = tens,
                    InnerTens = innerTens
                };

                allSeriesPerformance.Add(seriesPerf);

                // Aggregate daily stats
                if (!dailyStats.ContainsKey(sessionDate))
                {
                    dailyStats[sessionDate] = (0, 0, 0);
                }
                var current = dailyStats[sessionDate];
                dailyStats[sessionDate] = (current.totalScore + seriesScore, current.totalShots + group.Shots.Count, current.sessionCount + 1);
            }

            // Calculate group size from all shots in the session
            var allShots = shotsData.Groups
                .Where(g => g.ValueType == "10-shot-series")
                .SelectMany(g => g.Shots)
                .ToList();

            if (allShots.Count >= 2)
            {
                double maxDist = 0;
                for (int i = 0; i < allShots.Count; i++)
                {
                    for (int j = i + 1; j < allShots.Count; j++)
                    {
                        var dist = Math.Sqrt(Math.Pow(allShots[i].X - allShots[j].X, 2) + Math.Pow(allShots[i].Y - allShots[j].Y, 2));
                        maxDist = Math.Max(maxDist, dist);
                    }
                }
                statistics.AverageGroupSize = (statistics.AverageGroupSize * (statistics.TotalSessions - 1) + maxDist) / statistics.TotalSessions;
            }
        }

        // Calculate overall statistics
        if (allSeriesPerformance.Any())
        {
            statistics.AverageScore = allSeriesPerformance.Average(s => s.AveragePerShot);
            statistics.BestSeriesScore = allSeriesPerformance.Max(s => s.Score);
            statistics.WorstSeriesScore = allSeriesPerformance.Min(s => s.Score);
            statistics.RecentSeries = allSeriesPerformance.OrderByDescending(s => s.Date).Take(20).ToList();

            // Calculate improvement (compare first half vs second half)
            var orderedSeries = allSeriesPerformance.OrderBy(s => s.Date).ToList();
            if (orderedSeries.Count >= 4)
            {
                var halfPoint = orderedSeries.Count / 2;
                var firstHalfAvg = orderedSeries.Take(halfPoint).Average(s => s.AveragePerShot);
                var secondHalfAvg = orderedSeries.Skip(halfPoint).Average(s => s.AveragePerShot);
                statistics.ImprovementPercent = ((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100;
            }

            // Calculate consistency (standard deviation)
            var avgScore = allSeriesPerformance.Average(s => s.AveragePerShot);
            var variance = allSeriesPerformance.Sum(s => Math.Pow(s.AveragePerShot - avgScore, 2)) / allSeriesPerformance.Count;
            var stdDev = Math.Sqrt(variance);
            // Consistency score: 100 = perfect consistency, lower = more variation
            statistics.ConsistencyScore = Math.Max(0, 100 - (stdDev * 20));
        }

        // Daily performance
        statistics.DailyPerformance = dailyStats
            .Select(kvp => new DailyPerformance
            {
                Date = kvp.Key,
                AverageScore = kvp.Value.totalShots > 0 ? kvp.Value.totalScore / kvp.Value.totalShots : 0,
                SessionCount = kvp.Value.sessionCount,
                TotalShots = kvp.Value.totalShots
            })
            .OrderBy(d => d.Date)
            .ToList();

        return statistics;
    }

    private object GenerateChartData(List<TrainingSession> sessions, PerformanceStatistics statistics)
    {
        var labels = statistics.DailyPerformance.Select(d => d.Date.ToString("MMM dd")).ToList();
        var scores = statistics.DailyPerformance.Select(d => Math.Round(d.AverageScore, 2)).ToList();
        var shotCounts = statistics.DailyPerformance.Select(d => d.TotalShots).ToList();

        // Series progression
        var seriesLabels = statistics.RecentSeries.Select(s => $"{s.Date:MMM dd} S{s.SeriesNumber}").ToList();
        var seriesScores = statistics.RecentSeries.Select(s => Math.Round(s.Score, 1)).ToList();

        return new
        {
            dailyLabels = labels,
            dailyScores = scores,
            dailyShotCounts = shotCounts,
            seriesLabels = seriesLabels,
            seriesScores = seriesScores,
            tensProgression = statistics.RecentSeries.Select(s => s.Tens).ToList(),
            innerTensProgression = statistics.RecentSeries.Select(s => s.InnerTens).ToList()
        };
    }

    private object GenerateHeatMapData(List<TrainingSession> sessions)
    {
        var allShots = new List<object>();

        foreach (var session in sessions)
        {
            var shotsData = ParseShotsData(session.Shots);
            if (shotsData == null) continue;

            foreach (var group in shotsData.Groups.Where(g => g.ValueType == "10-shot-series"))
            {
                foreach (var shot in group.Shots)
                {
                    allShots.Add(new { x = shot.X, y = shot.Y });
                }
            }
        }

        return new { shots = allShots };
    }

    private ShotsData? ParseShotsData(string? shotsJson)
    {
        if (string.IsNullOrEmpty(shotsJson))
        {
            return null;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<ShotsData>(shotsJson, options);
        }
        catch
        {
            return null;
        }
    }

    private double CalculateShotScore(double x, double y)
    {
        var dist = Math.Sqrt(x * x + y * y);
        // Inner edge of pellet determines score (inward gauging)
        var innerEdge = Math.Max(0, dist - PELLET_RADIUS);

        // Ring 10 - inner edge must be within RING_10_BOUNDARY (0.25mm)
        // Score 10.0 to 10.9 based on shot CENTER distance
        if (innerEdge <= RING_10_BOUNDARY)
        {
            // At shot center = 0mm: 10.9
            // At shot center = 2.5mm: 10.0
            // Linear interpolation
            var maxCenterDistForTen = 2.5;
            var positionRatio = Math.Min(dist / maxCenterDistForTen, 1.0);
            var decimalScore = 10.9 - (positionRatio * 0.9);
            return Math.Round(decimalScore, 1);
        }

        // Rings 9 down to 1
        double innerRadius = RING_10_BOUNDARY;

        for (int ring = 9; ring >= 1; ring--)
        {
            var outerRadius = RingRadii[9 - ring];

            if (innerEdge <= outerRadius)
            {
                var positionInRing = (innerEdge - innerRadius) / (outerRadius - innerRadius);
                var decimalScore = (ring + 0.9) - (positionInRing * 0.9);
                return Math.Round(decimalScore, 1);
            }

            innerRadius = outerRadius;
        }

        // Outside ring 1 (miss)
        return 0;
    }
}
