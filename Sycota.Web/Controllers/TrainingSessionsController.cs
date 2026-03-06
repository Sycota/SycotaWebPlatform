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
public class TrainingSessionsController : Controller
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IClubMemberRepository _clubMemberRepository;
    private readonly IClubRepository _clubRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public TrainingSessionsController(
        ITrainingSessionRepository trainingSessionRepository,
        IClubMemberRepository clubMemberRepository,
        IClubRepository clubRepository,
        UserManager<ApplicationUser> userManager)
    {
        _trainingSessionRepository = trainingSessionRepository;
        _clubMemberRepository = clubMemberRepository;
        _clubRepository = clubRepository;
        _userManager = userManager;
    }

    // GET: /TrainingSessions/Create/5 (clubId)
    public async Task<IActionResult> Create(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, id, ClubMemberIncludeOptions.Club);
        if (membership == null)
        {
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction("Index", "Clubs");
        }

        var viewModel = new CreateTrainingSessionViewModel
        {
            ClubId = id,
            SessionDate = DateTime.Now,
            WeaponType = ISSFWeaponType.AirRifle
        };

        ViewBag.ClubName = membership.Club.Name;
        return View(viewModel);
    }

    // POST: /TrainingSessions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTrainingSessionViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, model.ClubId);
        if (membership == null)
        {
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction("Index", "Clubs");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var session = new TrainingSession
        {
            ClubId = model.ClubId,
            Name = model.Name,
            Description = model.Description,
            SessionDate = model.SessionDate,
            WeaponType = model.WeaponType,
            Notes = model.Notes,
            Shots = "{\"warmupShots\":[],\"groups\":[]}",
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _trainingSessionRepository.AddTrainingSessionAsync(session);

        return RedirectToAction(nameof(RecordShots), new { id = session.Id });
    }

    // GET: /TrainingSessions/RecordShots/5
    public async Task<IActionResult> RecordShots(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(id, TrainingSessionIncludeOptions.All);
        if (session == null)
        {
            return NotFound();
        }

        // Check if user can edit this session (owner or trainer of the owner)
        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, session.ClubId, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction("Index", "Clubs");
        }

        var canEdit = session.CreatedById == userId;
        if (!canEdit && membership.CanTrain)
        {
            // Trainers can edit sessions of their trainees
            var sessionOwnerMembership = await _clubMemberRepository.GetByUserAndClubAsync(session.CreatedById, session.ClubId);
            canEdit = sessionOwnerMembership?.TrainerId == membership.Id;
        }

        if (!canEdit)
        {
            TempData["Error"] = "Нямате право да редактирате тази сесия.";
            return RedirectToAction("Details", new { id });
        }

        var viewModel = new RecordShotsViewModel
        {
            ClubId = session.ClubId,
            SessionId = session.Id,
            Session = session,
            ShotsJson = session.Shots ?? "{\"warmupShots\":[],\"groups\":[]}"
        };

        return View(viewModel);
    }

    // POST: /TrainingSessions/SaveShots
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShots(int id, string shotsJson)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        // Check permissions
        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, session.ClubId, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            return Unauthorized();
        }

        var canEdit = session.CreatedById == userId;
        if (!canEdit && membership.CanTrain)
        {
            var sessionOwnerMembership = await _clubMemberRepository.GetByUserAndClubAsync(session.CreatedById, session.ClubId);
            canEdit = sessionOwnerMembership?.TrainerId == membership.Id;
        }

        if (!canEdit)
        {
            return Unauthorized();
        }

        session.Shots = shotsJson;
        await _trainingSessionRepository.UpdateTrainingSessionAsync(session);

        TempData["Success"] = "Изстрелите са запазени успешно.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /TrainingSessions/SaveShotsAjax (AJAX endpoint)
    [HttpPost]
    public async Task<IActionResult> SaveShotsAjax([FromBody] SaveShotsRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Неоторизиран достъп" });
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(request.SessionId);
        if (session == null)
        {
            return Json(new { success = false, message = "Сесията не е намерена" });
        }

        // Check permissions
        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, session.ClubId, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            return Json(new { success = false, message = "Не сте член" });
        }

        var canEdit = session.CreatedById == userId;
        if (!canEdit && membership.CanTrain)
        {
            var sessionOwnerMembership = await _clubMemberRepository.GetByUserAndClubAsync(session.CreatedById, session.ClubId);
            canEdit = sessionOwnerMembership?.TrainerId == membership.Id;
        }

        if (!canEdit)
        {
            return Json(new { success = false, message = "Нямате право" });
        }

        session.Shots = request.ShotsJson;
        await _trainingSessionRepository.UpdateTrainingSessionAsync(session);

        return Json(new { success = true });
    }

    // GET: /TrainingSessions/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(id, TrainingSessionIncludeOptions.All);
        if (session == null)
        {
            return NotFound();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, session.ClubId, ClubMemberIncludeOptions.All);
        if (membership == null)
        {
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction("Index", "Clubs");
        }

        var canEdit = session.CreatedById == userId;
        if (!canEdit && membership.CanTrain)
        {
            var sessionOwnerMembership = await _clubMemberRepository.GetByUserAndClubAsync(session.CreatedById, session.ClubId);
            canEdit = sessionOwnerMembership?.TrainerId == membership.Id;
        }

        var viewModel = new TrainingSessionDetailsViewModel
        {
            Session = session,
            CurrentMembership = membership,
            CanEdit = canEdit
        };

        return View(viewModel);
    }

    // POST: /TrainingSessions/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        if (session.CreatedById != userId)
        {
            TempData["Error"] = "Можете да изтривате само собствените си сесии.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var clubId = session.ClubId;
        await _trainingSessionRepository.DeleteTrainingSessionAsync(session);

        TempData["Success"] = "Тренировъчната сесия е изтрита.";
        return RedirectToAction("MyResults", "Clubs", new { id = clubId });
    }
}

public class SaveShotsRequest
{
    public int SessionId { get; set; }
    public string ShotsJson { get; set; } = string.Empty;
}
