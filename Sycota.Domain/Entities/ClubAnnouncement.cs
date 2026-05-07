namespace Sycota.Domain.Entities;

public class ClubAnnouncement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ClubId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
