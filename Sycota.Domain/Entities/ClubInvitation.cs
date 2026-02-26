using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

/// <summary>
/// Represents an invitation from a club admin to invite a user to join.
/// </summary>
public class ClubInvitation
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Email { get; set; } = string.Empty;
    public ClubRole OfferedRole { get; set; }
    public int? AssignedTrainerId { get; set; }
    public string? Message { get; set; }
    public MembershipRequestStatus Status { get; set; } = MembershipRequestStatus.Pending;
    public string InvitationCode { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedByUserId { get; set; }

    // Navigation properties
    public Club Club { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ClubMember? AssignedTrainer { get; set; }
    public ApplicationUser? AcceptedByUser { get; set; }
}
