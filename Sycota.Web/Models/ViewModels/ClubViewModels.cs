using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Web.Models.ViewModels;

public class ClubIndexViewModel
{
    public IEnumerable<Club> Clubs { get; set; } = [];
    public ClubMember? CurrentMembership { get; set; }
}

public class ClubDetailsViewModel
{
    public Club Club { get; set; } = null!;
    public ClubMember? CurrentMembership { get; set; }
    public IEnumerable<ClubMember> Members { get; set; } = [];
    public IEnumerable<ClubMember> Trainers { get; set; } = [];
    public IEnumerable<ClubMember> Competitors { get; set; } = [];
    public int PendingRequestsCount { get; set; }
    public bool HasPendingRequest { get; set; }
    public bool IsTrainer => CurrentMembership?.CanTrain == true;
    public bool IsAdmin => CurrentMembership?.Role == ClubRole.Admin;
    public bool IsCompetitor => CurrentMembership?.Role == ClubRole.Competitor;
}

public class MyResultsViewModel
{
    public ClubMember CurrentMembership { get; set; } = null!;
    public IEnumerable<TrainingSession> TrainingSessions { get; set; } = [];
    public ShooterProfile? ShooterProfile { get; set; }
}

public class TraineesViewModel
{
    public ClubMember TrainerMembership { get; set; } = null!;
    public IEnumerable<ClubMember> Trainees { get; set; } = [];
}

public class TraineeDetailsViewModel
{
    public ClubMember Trainee { get; set; } = null!;
    public IEnumerable<TrainingSession> TrainingSessions { get; set; } = [];
    public ShooterProfile? ShooterProfile { get; set; }
}

public class CreateClubViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool RequiresApproval { get; set; } = true;
}

public class JoinClubViewModel
{
    public int ClubId { get; set; }
    public ClubRole RequestedRole { get; set; } = ClubRole.Competitor;
    public int? TrainerId { get; set; }
    public string? Message { get; set; }
    public IEnumerable<ClubMember> AvailableTrainers { get; set; } = [];
}

public class ManageRequestsViewModel
{
    public Club Club { get; set; } = null!;
    public IEnumerable<ClubJoinRequest> PendingRequests { get; set; } = [];
    public IEnumerable<ClubInvitation> PendingInvitations { get; set; } = [];
}

public class CreateInvitationViewModel
{
    public int ClubId { get; set; }
    public string Email { get; set; } = string.Empty;
    public ClubRole OfferedRole { get; set; } = ClubRole.Competitor;
    public int? TrainerId { get; set; }
    public string? Message { get; set; }
    public int ExpirationDays { get; set; } = 7;
    public IEnumerable<ClubMember> AvailableTrainers { get; set; } = [];
}

public class AcceptInvitationViewModel
{
    public ClubInvitation Invitation { get; set; } = null!;
}

public class NotificationsViewModel
{
    public IEnumerable<ClubInvitation> PendingInvitations { get; set; } = [];
    public int TotalCount => PendingInvitations.Count();
}

public class ManageMembersViewModel
{
    public Club Club { get; set; } = null!;
    public IEnumerable<ClubMember> Trainers { get; set; } = [];
    public IEnumerable<ClubMember> Competitors { get; set; } = [];
    public IEnumerable<ClubMember> Admins { get; set; } = [];
}

public class AssignTrainerViewModel
{
    public int ClubId { get; set; }
    public int CompetitorId { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public int? CurrentTrainerId { get; set; }
    public string? CurrentTrainerName { get; set; }
    public int? SelectedTrainerId { get; set; }
    public IEnumerable<ClubMember> AvailableTrainers { get; set; } = [];
}

// Training Session ViewModels
public class CreateTrainingSessionViewModel
{
    public int ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime SessionDate { get; set; } = DateTime.Now;
    public ISSFWeaponType WeaponType { get; set; } = ISSFWeaponType.AirRifle;
    public string? Notes { get; set; }
}

public class RecordShotsViewModel
{
    public int ClubId { get; set; }
    public int SessionId { get; set; }
    public TrainingSession Session { get; set; } = null!;
    public string ShotsJson { get; set; } = "{}";
}

public class TrainingSessionDetailsViewModel
{
    public TrainingSession Session { get; set; } = null!;
    public ClubMember? CurrentMembership { get; set; }
    public bool CanEdit { get; set; }
}
