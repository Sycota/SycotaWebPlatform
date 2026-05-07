using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
    private readonly IClubAnnouncementRepository _clubAnnouncementRepository;
    private readonly IClubInventoryRepository _clubInventoryRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

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
        IClubAnnouncementRepository clubAnnouncementRepository,
        IClubInventoryRepository clubInventoryRepository,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
    {
        _clubRepository = clubRepository;
        _clubMemberRepository = clubMemberRepository;
        _clubService = clubService;
        _trainingSessionRepository = trainingSessionRepository;
        _clubAnnouncementRepository = clubAnnouncementRepository;
        _clubInventoryRepository = clubInventoryRepository;
        _userManager = userManager;
        _emailSender = emailSender;
    }

    // GET: /Clubs
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var myMemberships = (await _clubMemberRepository.GetAllClubMembersAsync())
            .Where(m => m.UserId == userId)
            .ToList();

        var myClubIds = myMemberships.Select(m => m.ClubId).Distinct().ToHashSet();
        var clubs = (await _clubRepository.GetAllClubsAsync(ClubIncludeOptions.Members))
            .Where(c => myClubIds.Contains(c.Id))
            .ToList();

        var viewModel = new ClubIndexViewModel
        {
            Clubs = clubs,
            CurrentMembership = null
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

        var announcements = await _clubAnnouncementRepository.GetByClubIdAsync(id);

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
            Announcements = announcements,
            NewAnnouncement = new CreateAnnouncementViewModel { ClubId = id },
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
                TempData["Error"] = "Вече сте член на този клуб.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var pendingResult = await _clubService.HasPendingJoinRequestAsync(userId, id);
            if (pendingResult.Success && pendingResult.Data)
            {
                TempData["Error"] = "Вече имате чакаща заявка за присъединяване към този клуб.";
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

            TempData["Success"] = "Заявката ви за присъединяване към клуба е изпратена. Администратор ще я прегледа скоро.";
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

            TempData["Success"] = "Успешно се присъединихте към клуба!";
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
            TempData["Error"] = "Само администратори могат да управляват заявките за присъединяване.";
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
            TempData["Error"] = "Само администратори могат да одобряват заявки.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _clubService.ApproveJoinRequestAsync(requestId, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Заявката е одобрена успешно.";
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
            TempData["Error"] = "Само администратори могат да отхвърлят заявки.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _clubService.RejectJoinRequestAsync(requestId, userId, reason);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Заявката е отхвърлена.";
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
            TempData["Error"] = "Само администратори могат да канят членове.";
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
            TempData["Error"] = "Само администратори могат да канят членове.";
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

        try
        {
            var club = await _clubRepository.GetClubByIdAsync(model.ClubId);
            var invitation = await _clubService.GetPendingInvitationsAsync(model.ClubId);
            var latestInvitation = invitation.Success
                ? invitation.Data.FirstOrDefault(i => i.Email == model.Email)
                : null;

            if (latestInvitation is not null)
            {
                var invitationUrl = Url.Action(nameof(AcceptInvitation), "Clubs", new { code = latestInvitation.InvitationCode }, Request.Scheme);
                var safeClubName = System.Net.WebUtility.HtmlEncode(club?.Name ?? "SYCOTA+");
                var safeRole = System.Net.WebUtility.HtmlEncode(latestInvitation.OfferedRole.ToString());
                var safeMessage = string.IsNullOrWhiteSpace(latestInvitation.Message)
                    ? string.Empty
                    : $"<p><strong>Съобщение:</strong> {System.Net.WebUtility.HtmlEncode(latestInvitation.Message)}</p>";
                var messageBody = $@"
                    <p>Получихте покана за клуб <strong>{safeClubName}</strong> в SYCOTA+.</p>
                    <p><strong>Роля:</strong> {safeRole}</p>
                    {safeMessage}
                    <p><a href='{invitationUrl}'>Натиснете тук, за да прегледате и приемете поканата</a></p>
                    <p>Поканата е валидна до: {latestInvitation.ExpiresAt:dd.MM.yyyy HH:mm}</p>";

                await _emailSender.SendEmailAsync(model.Email, "Покана за клуб в SYCOTA+", messageBody);
            }
        }
        catch
        {
            TempData["Error"] = "Поканата е създадена, но изпращането на имейл не беше успешно.";
        }

        TempData["Success"] = $"Поканата е изпратена до {model.Email}";
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
            TempData["Error"] = "Тази покана е изтекла.";
            return RedirectToAction(nameof(Index));
        }

        if (invitation.Status != MembershipRequestStatus.Pending)
        {
            TempData["Error"] = "Тази покана вече е използвана или отменена.";
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
        TempData["Success"] = "Успешно се присъединихте към клуба!";
        
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
            TempData["Error"] = "Само администратори могат да отменят покани.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _clubService.CancelInvitationAsync(invitationId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Поканата е отменена.";
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
            TempData["Error"] = "Не сте член на този клуб.";
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
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction(nameof(Index));
        }

        if (!membership.CanTrain)
        {
            TempData["Error"] = "Само треньори могат да преглеждат обучаеми.";
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
            TempData["Error"] = "Трябва да сте треньор, за да преглеждате детайли за обучаеми.";
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
            TempData["Error"] = "Този състезател не е назначен към вас.";
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
            TempData["Error"] = "Трябва да сте треньор, за да преглеждате представянето на обучаеми.";
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
            TempData["Error"] = "Този състезател не е назначен към вас.";
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
            TempData["Error"] = "Трябва да сте треньор, за да преглеждате сесиите на обучаеми.";
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
            TempData["Error"] = "Този състезател не е назначен към вас.";
            return RedirectToAction(nameof(Trainees), new { id });
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(sessionId, TrainingSessionIncludeOptions.All);
        if (session == null || session.CreatedById != trainee.UserId)
        {
            TempData["Error"] = "Сесията не е намерена или не принадлежи на този обучаем.";
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
            TempData["Error"] = "Само администратори могат да редактират данните на клуба.";
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
            TempData["Error"] = "Само администратори могат да редактират данните на клуба.";
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

        TempData["Success"] = "Данните на клуба са обновени успешно.";
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
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _clubService.RemoveClubMemberAsync(membership.Id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Напуснахте клуба.";
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
            TempData["Error"] = "Само администратори могат да управляват членовете.";
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

    // GET: /Clubs/Inventory/5
    public async Task<IActionResult> Inventory(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction(nameof(Index));
        }

        var club = await _clubRepository.GetClubByIdAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var competitorsResult = await _clubService.GetCompetitorsAsync(id, ClubMemberIncludeOptions.User);
        var viewModel = new ClubInventoryViewModel
        {
            Club = club,
            CurrentMembership = membership,
            Weapons = await _clubInventoryRepository.GetWeaponsByClubIdAsync(id),
            AmmoBatches = await _clubInventoryRepository.GetAmmoByClubIdAsync(id),
            Issues = await _clubInventoryRepository.GetIssuesByClubIdAsync(id),
            Shooters = competitorsResult.Success ? competitorsResult.Data : []
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWeapon(AddWeaponViewModel request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, request.ClubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Само администратори могат да добавят оръжия.";
            return RedirectToAction(nameof(Inventory), new { id = request.ClubId });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Невалидни данни за оръжие.";
            return RedirectToAction(nameof(Inventory), new { id = request.ClubId });
        }

        await _clubInventoryRepository.AddWeaponAsync(new ClubWeapon
        {
            ClubId = request.ClubId,
            SerialNumber = request.SerialNumber.Trim(),
            Model = request.Model.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        TempData["Success"] = "Оръжието е добавено успешно.";
        return RedirectToAction(nameof(Inventory), new { id = request.ClubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditWeapon(EditWeaponViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Само администратори могат да редактират оръжия.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        var weapon = await _clubInventoryRepository.GetWeaponByIdAsync(model.Id);
        if (weapon == null || weapon.ClubId != model.ClubId)
        {
            TempData["Error"] = "Оръжието не е намерено.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Невалидни данни за оръжие.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        weapon.SerialNumber = model.SerialNumber.Trim();
        weapon.Model = model.Model.Trim();
        weapon.AssignedShooterId = model.AssignedShooterId;

        await _clubInventoryRepository.UpdateWeaponAsync(weapon);
        TempData["Success"] = "Оръжието е обновено успешно.";
        return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWeapon(int clubId, int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, clubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Само администратори могат да изтриват оръжия.";
            return RedirectToAction(nameof(Inventory), new { id = clubId });
        }

        var weapon = await _clubInventoryRepository.GetWeaponByIdAsync(id);
        if (weapon == null || weapon.ClubId != clubId)
        {
            TempData["Error"] = "Оръжието не е намерено.";
            return RedirectToAction(nameof(Inventory), new { id = clubId });
        }

        try
        {
            await _clubInventoryRepository.DeleteWeaponAsync(weapon);
            TempData["Success"] = "Оръжието е изтрито успешно.";
        }
        catch
        {
            TempData["Error"] = "Оръжието не може да бъде изтрито, защото има история на издавания.";
        }
        return RedirectToAction(nameof(Inventory), new { id = clubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAmmo(AddAmmoViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Само администратори могат да добавят боеприпаси.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Невалидни данни за боеприпас.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        await _clubInventoryRepository.AddAmmoAsync(new ClubAmmo
        {
            ClubId = model.ClubId,
            SerialNumber = model.SerialNumber.Trim(),
            Type = model.Type,
            Quantity = model.Quantity,
            RemainingQuantity = model.Quantity,
            CreatedAt = DateTime.UtcNow
        });

        TempData["Success"] = "Боеприпасът е добавен успешно.";
        return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAmmo(EditAmmoViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Само администратори могат да редактират патрони.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        var ammo = await _clubInventoryRepository.GetAmmoByIdAsync(model.Id);
        if (ammo == null || ammo.ClubId != model.ClubId)
        {
            TempData["Error"] = "Патроните не са намерени.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        if (!ModelState.IsValid || model.RemainingQuantity > model.Quantity)
        {
            TempData["Error"] = "Невалидни данни за патрони.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        ammo.SerialNumber = model.SerialNumber.Trim();
        ammo.Type = model.Type;
        ammo.Quantity = model.Quantity;
        ammo.RemainingQuantity = model.RemainingQuantity;

        await _clubInventoryRepository.UpdateAmmoAsync(ammo);
        TempData["Success"] = "Патроните са обновени успешно.";
        return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAmmo(int clubId, int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, clubId);
        if (membership == null || membership.Role != ClubRole.Admin)
        {
            TempData["Error"] = "Само администратори могат да изтриват патрони.";
            return RedirectToAction(nameof(Inventory), new { id = clubId });
        }

        var ammo = await _clubInventoryRepository.GetAmmoByIdAsync(id);
        if (ammo == null || ammo.ClubId != clubId)
        {
            TempData["Error"] = "Патроните не са намерени.";
            return RedirectToAction(nameof(Inventory), new { id = clubId });
        }

        try
        {
            await _clubInventoryRepository.DeleteAmmoAsync(ammo);
            TempData["Success"] = "Патроните са изтрити успешно.";
        }
        catch
        {
            TempData["Error"] = "Патроните не могат да бъдат изтрити, защото има история на издавания.";
        }
        return RedirectToAction(nameof(Inventory), new { id = clubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueWeapon(IssueWeaponViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId, ClubMemberIncludeOptions.All);
        if (membership == null || !membership.CanTrain)
        {
            TempData["Error"] = "Само треньори могат да издават оръжие.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        var weapon = await _clubInventoryRepository.GetWeaponByIdAsync(model.WeaponId);
        var shooter = await _clubMemberRepository.GetClubMemberByIdAsync(model.ShooterId);
        if (weapon == null || shooter == null || weapon.ClubId != model.ClubId || shooter.ClubId != model.ClubId)
        {
            TempData["Error"] = "Невалидни данни за издаване на оръжие.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        weapon.AssignedShooterId = shooter.Id;
        await _clubInventoryRepository.UpdateWeaponAsync(weapon);

        await _clubInventoryRepository.AddIssueAsync(new InventoryIssue
        {
            ClubId = model.ClubId,
            ShooterId = shooter.Id,
            IssuedById = membership.Id,
            WeaponId = weapon.Id,
            IssuedAt = DateTime.UtcNow
        });

        TempData["Success"] = "Оръжието е издадено успешно.";
        return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueAmmo(IssueAmmoViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId, ClubMemberIncludeOptions.All);
        if (membership == null || !membership.CanTrain)
        {
            TempData["Error"] = "Само треньори могат да издават боеприпаси.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        var ammo = await _clubInventoryRepository.GetAmmoByIdAsync(model.AmmoId);
        var shooter = await _clubMemberRepository.GetClubMemberByIdAsync(model.ShooterId);
        if (ammo == null || shooter == null || ammo.ClubId != model.ClubId || shooter.ClubId != model.ClubId)
        {
            TempData["Error"] = "Невалидни данни за издаване на боеприпас.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        if (model.Quantity <= 0 || ammo.RemainingQuantity < model.Quantity)
        {
            TempData["Error"] = "Недостатъчно количество в избрания боеприпас.";
            return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
        }

        ammo.RemainingQuantity -= model.Quantity;
        await _clubInventoryRepository.UpdateAmmoAsync(ammo);

        await _clubInventoryRepository.AddIssueAsync(new InventoryIssue
        {
            ClubId = model.ClubId,
            ShooterId = shooter.Id,
            IssuedById = membership.Id,
            AmmoId = ammo.Id,
            AmmoQuantity = model.Quantity,
            IssuedAt = DateTime.UtcNow
        });

        TempData["Success"] = "Боеприпасът е издаден успешно.";
        return RedirectToAction(nameof(Inventory), new { id = model.ClubId });
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
            TempData["Error"] = "Само администратори могат да назначават треньори.";
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
            TempData["Error"] = "Невалиден избор на състезател.";
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var trainersResult = await _clubService.GetTrainersAsync(id, ClubMemberIncludeOptions.User);

        var competitorName = !string.IsNullOrEmpty(competitor.User?.FirstName) 
            ? $"{competitor.User.FirstName} {competitor.User.LastName}" 
            : competitor.User?.UserName ?? "Неизвестен";

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
            TempData["Error"] = "Само администратори могат да назначават треньори.";
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
            TempData["Success"] = "Треньорът е назначен успешно.";
        }
        else
        {
            TempData["Success"] = "Назначението на треньор е премахнато.";
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
            TempData["Error"] = "Само администратори могат да премахват членове.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (membership.Id == memberId)
        {
            TempData["Error"] = "Не можете да премахнете себе си. Използвайте опцията за напускане на клуба.";
            return RedirectToAction(nameof(ManageMembers), new { id });
        }

        var result = await _clubService.RemoveClubMemberAsync(memberId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Членът е премахнат от клуба.";
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
            TempData["Error"] = "Само администратори могат да променят треньорския статус.";
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
            TempData["Error"] = "Треньорският статус може да се променя само за администратори в този клуб.";
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
                ? "Треньорската роля е деактивирана за този администратор." 
                : "Треньорската роля е активирана за този администратор.";
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
            TempData["Error"] = "Не сте член на този клуб.";
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

    // POST: /Clubs/Announcement
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(CreateAnnouncementViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Моля, попълнете коректно заглавието и съдържанието на съобщението.";
            return RedirectToAction(nameof(Details), new { id = model.ClubId });
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId, ClubMemberIncludeOptions.User);
        if (membership == null || (membership.Role != ClubRole.Admin && membership.Role != ClubRole.Trainer))
        {
            TempData["Error"] = "Само администратори и треньори могат да публикуват съобщения.";
            return RedirectToAction(nameof(Details), new { id = model.ClubId });
        }

        var authorName = !string.IsNullOrWhiteSpace(membership.User?.FirstName)
            ? $"{membership.User.FirstName} {membership.User.LastName}".Trim()
            : membership.User?.UserName ?? "Неизвестен";

        var announcement = new ClubAnnouncement
        {
            ClubId = model.ClubId,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            CreatedByUserId = userId,
            CreatedByName = authorName,
            CreatedAt = DateTime.UtcNow
        };

        await _clubAnnouncementRepository.AddAsync(announcement);
        TempData["Success"] = "Съобщението е публикувано успешно.";

        return RedirectToAction(nameof(Details), new { id = model.ClubId });
    }
}
