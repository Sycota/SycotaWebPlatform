namespace Sycota.Domain.Entities;

public class AiChatMessage
{
    public int Id { get; set; }
    public int TrainingSessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TrainingSession TrainingSession { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
