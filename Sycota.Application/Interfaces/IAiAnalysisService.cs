using Sycota.Domain.Classes;

namespace Sycota.Application.Interfaces;

public interface IAiAnalysisService
{
    /// <summary>
    /// Analyzes a training session and provides initial AI-generated insights
    /// </summary>
    Task<ServiceResult<string>> AnalyzeSessionAsync(string sessionJson, string weaponType, string sessionName, DateTime sessionDate);

    /// <summary>
    /// Sends a chat message to the AI and gets a response in the context of a training session
    /// </summary>
    Task<ServiceResult<string>> ChatAsync(string userMessage, string sessionContext, List<ChatMessage> conversationHistory);
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}
