using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AiAnalysisController : ControllerBase
{
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IClubMemberRepository _clubMemberRepository;
    private readonly IAiChatMessageRepository _chatMessageRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AiAnalysisController(
        IAiAnalysisService aiAnalysisService,
        ITrainingSessionRepository trainingSessionRepository,
        IClubMemberRepository clubMemberRepository,
        IAiChatMessageRepository chatMessageRepository,
        UserManager<ApplicationUser> userManager)
    {
        _aiAnalysisService = aiAnalysisService;
        _trainingSessionRepository = trainingSessionRepository;
        _clubMemberRepository = clubMemberRepository;
        _chatMessageRepository = chatMessageRepository;
        _userManager = userManager;
    }

    [HttpPost("analyze/{sessionId}")]
    public async Task<IActionResult> AnalyzeSession(int sessionId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Не сте удостоверени" });
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(sessionId, TrainingSessionIncludeOptions.All);
        if (session == null)
        {
            return NotFound(new { error = "Сесията не е намерена" });
        }

        // Check access
        var hasAccess = await CheckSessionAccessAsync(userId, session);
        if (!hasAccess)
        {
            return Forbid();
        }

        // Check if we already have an analysis for this session
        var existingMessages = await _chatMessageRepository.GetBySessionIdAsync(sessionId, 1);
        var existingAnalysis = existingMessages.FirstOrDefault(m => m.Role == "assistant");
        
        if (existingAnalysis != null)
        {
            // Return cached analysis
            return Ok(new { analysis = existingAnalysis.Content, cached = true });
        }

        var result = await _aiAnalysisService.AnalyzeSessionAsync(
            session.Shots ?? "{}",
            session.WeaponType.ToString(),
            session.Name,
            session.SessionDate
        );

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        // Save the analysis to the database
        await _chatMessageRepository.AddAsync(new AiChatMessage
        {
            TrainingSessionId = sessionId,
            UserId = userId,
            Role = "assistant",
            Content = result.Data,
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new { analysis = result.Data, cached = false });
    }

    [HttpPost("chat/{sessionId}")]
    public async Task<IActionResult> Chat(int sessionId, [FromBody] ChatRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Не сте удостоверени" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Съобщението е задължително" });
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(sessionId, TrainingSessionIncludeOptions.All);
        if (session == null)
        {
            return NotFound(new { error = "Сесията не е намерена" });
        }

        // Check access
        var hasAccess = await CheckSessionAccessAsync(userId, session);
        if (!hasAccess)
        {
            return Forbid();
        }

        // Get existing chat history from database (last 20 messages for context)
        var dbHistory = await _chatMessageRepository.GetBySessionIdAsync(sessionId, 20);
        var history = dbHistory.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        // Build session context
        var sessionContext = $@"Session: {session.Name}
Date: {session.SessionDate:MMMM dd, yyyy}
Weapon: {session.WeaponType}
Shot Data: {session.Shots ?? "{}"}";

        var result = await _aiAnalysisService.ChatAsync(
            request.Message,
            sessionContext,
            history
        );

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        // Save both user message and AI response to database
        var messagesToSave = new List<AiChatMessage>
        {
            new AiChatMessage
            {
                TrainingSessionId = sessionId,
                UserId = userId,
                Role = "user",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow
            },
            new AiChatMessage
            {
                TrainingSessionId = sessionId,
                UserId = userId,
                Role = "assistant",
                Content = result.Data,
                CreatedAt = DateTime.UtcNow.AddMilliseconds(1) // Ensure ordering
            }
        };

        await _chatMessageRepository.AddRangeAsync(messagesToSave);

        return Ok(new { response = result.Data });
    }

    [HttpGet("history/{sessionId}")]
    public async Task<IActionResult> GetChatHistory(int sessionId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Не сте удостоверени" });
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Сесията не е намерена" });
        }

        // Check access
        var hasAccess = await CheckSessionAccessAsync(userId, session);
        if (!hasAccess)
        {
            return Forbid();
        }

        var messages = await _chatMessageRepository.GetBySessionIdAsync(sessionId);
        var history = messages.Select(m => new
        {
            role = m.Role,
            content = m.Content,
            createdAt = m.CreatedAt
        });

        return Ok(new { messages = history });
    }

    [HttpDelete("history/{sessionId}")]
    public async Task<IActionResult> ClearChatHistory(int sessionId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Не сте удостоверени" });
        }

        var session = await _trainingSessionRepository.GetTrainingSessionByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Сесията не е намерена" });
        }

        // Only owner can clear history
        if (session.CreatedById != userId)
        {
            return Forbid();
        }

        await _chatMessageRepository.DeleteBySessionIdAsync(sessionId);

        return Ok(new { success = true });
    }

    private async Task<bool> CheckSessionAccessAsync(string userId, TrainingSession session)
    {
        // Owner always has access
        if (session.CreatedById == userId)
        {
            return true;
        }

        // Check if user is a trainer of the session owner
        var membership = await _clubMemberRepository.GetByUserAndClubAsync(userId, session.ClubId, ClubMemberIncludeOptions.All);
        if (membership != null && membership.CanTrain)
        {
            var ownerMembership = await _clubMemberRepository.GetByUserAndClubAsync(session.CreatedById, session.ClubId);
            if (ownerMembership?.TrainerId == membership.Id)
            {
                return true;
            }
        }

        return false;
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}
