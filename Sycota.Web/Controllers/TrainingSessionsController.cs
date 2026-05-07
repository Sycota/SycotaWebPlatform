using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Web.Models.ViewModels;
using System.Globalization;
using System.Text;

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

    // GET: /TrainingSessions/ExportResultsCsv/5 (clubId)
    public async Task<IActionResult> ExportResultsCsv(int id)
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
            return RedirectToAction("Index", "Clubs");
        }

        var sessions = (await _trainingSessionRepository.GetAllTrainingSessionsByClubIdAsync(id, TrainingSessionIncludeOptions.None))
            .Where(s => s.CreatedById == userId)
            .OrderByDescending(s => s.SessionDate)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Name,SessionDate,WeaponType,Description,Notes");
        foreach (var s in sessions)
        {
            sb.AppendLine($"{EscapeCsv(s.Name)},{s.SessionDate:O},{EscapeCsv(s.WeaponType.ToString())},{EscapeCsv(s.Description)},{EscapeCsv(s.Notes)}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"sycota-results-{id}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // GET: /TrainingSessions/ExportResultsExcel/5 (clubId)
    public async Task<IActionResult> ExportResultsExcel(int id)
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
            return RedirectToAction("Index", "Clubs");
        }

        var sessions = (await _trainingSessionRepository.GetAllTrainingSessionsByClubIdAsync(id, TrainingSessionIncludeOptions.None))
            .Where(s => s.CreatedById == userId)
            .OrderByDescending(s => s.SessionDate)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("<table><tr><th>Име</th><th>Дата</th><th>Тип оръжие</th><th>Описание</th><th>Бележки</th></tr>");
        foreach (var s in sessions)
        {
            sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(s.Name)}</td><td>{s.SessionDate:dd.MM.yyyy HH:mm}</td><td>{System.Net.WebUtility.HtmlEncode(s.WeaponType.ToString())}</td><td>{System.Net.WebUtility.HtmlEncode(s.Description)}</td><td>{System.Net.WebUtility.HtmlEncode(s.Notes)}</td></tr>");
        }
        sb.AppendLine("</table>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "application/vnd.ms-excel", $"sycota-results-{id}-{DateTime.UtcNow:yyyyMMdd}.xls");
    }

    // POST: /TrainingSessions/ImportResultsCsv
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportResultsCsv(int clubId, IFormFile file)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, clubId);
        if (membership == null)
        {
            TempData["Error"] = "Не сте член на този клуб.";
            return RedirectToAction("Index", "Clubs");
        }

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Моля, изберете CSV файл за импорт.";
            return RedirectToAction("MyResults", "Clubs", new { id = clubId });
        }

        var imported = 0;
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        _ = await reader.ReadLineAsync(); // header
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = ParseCsvLine(line);
            if (columns.Count < 5) continue;

            if (!DateTime.TryParse(columns[1], null, DateTimeStyles.RoundtripKind, out var sessionDate))
            {
                continue;
            }

            var weaponType = ParseWeaponType(columns[2]);

            var session = new TrainingSession
            {
                ClubId = clubId,
                Name = columns[0],
                SessionDate = sessionDate,
                WeaponType = weaponType,
                Description = columns[3],
                Notes = columns[4],
                Shots = "{\"warmupShots\":[],\"groups\":[]}",
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _trainingSessionRepository.AddTrainingSessionAsync(session);
            imported++;
        }

        TempData["Success"] = $"Успешно импортирани сесии: {imported}.";
        return RedirectToAction("MyResults", "Clubs", new { id = clubId });
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

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private static ISSFWeaponType ParseWeaponType(string input)
    {
        if (Enum.TryParse<ISSFWeaponType>(input, true, out var parsed))
        {
            return parsed;
        }

        if (int.TryParse(input, out var intValue) && Enum.IsDefined(typeof(ISSFWeaponType), intValue))
        {
            return (ISSFWeaponType)intValue;
        }

        return ISSFWeaponType.AirRifle;
    }
}

public class SaveShotsRequest
{
    public int SessionId { get; set; }
    public string ShotsJson { get; set; } = string.Empty;
}
