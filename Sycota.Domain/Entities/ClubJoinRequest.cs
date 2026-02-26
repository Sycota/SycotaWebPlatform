using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

/// <summary>
/// Represents a request from a user to join a club, pending admin approval.
/// </summary>
public class ClubJoinRequest
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public ClubRole RequestedRole { get; set; }
    public int? RequestedTrainerId { get; set; }
    public string? Message { get; set; }
    public MembershipRequestStatus Status { get; set; } = MembershipRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedById { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public ClubMember? RequestedTrainer { get; set; }
    public ApplicationUser? ProcessedBy { get; set; }
}
