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
    public ClubMember TrainerMembership { get; set; } = null!;
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

// Performance Dashboard ViewModels
public class PerformanceDashboardViewModel
{
    public ClubMember CurrentMembership { get; set; } = null!;
    public IEnumerable<TrainingSession> TrainingSessions { get; set; } = [];
    public PerformanceStatistics Statistics { get; set; } = new();
    public string ChartDataJson { get; set; } = "{}";
    public string HeatMapDataJson { get; set; } = "{}";
    public int SelectedDays { get; set; } = 30;
}

public class PerformanceStatistics
{
    public int TotalSessions { get; set; }
    public int TotalShots { get; set; }
    public double AverageScore { get; set; }
    public double BestSeriesScore { get; set; }
    public double WorstSeriesScore { get; set; }
    public int TotalTens { get; set; }
    public int TotalInnerTens { get; set; }
    public double AverageGroupSize { get; set; }
    public double ImprovementPercent { get; set; }
    public List<SeriesPerformance> RecentSeries { get; set; } = [];
    public List<DailyPerformance> DailyPerformance { get; set; } = [];
    public double ConsistencyScore { get; set; }
}

public class SeriesPerformance
{
    public DateTime Date { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public int SeriesNumber { get; set; }
    public double Score { get; set; }
    public int ShotCount { get; set; }
    public double AveragePerShot { get; set; }
    public int Tens { get; set; }
    public int InnerTens { get; set; }
}

public class DailyPerformance
{
    public DateTime Date { get; set; }
    public double AverageScore { get; set; }
    public int SessionCount { get; set; }
    public int TotalShots { get; set; }
}

public class ShotPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class ShotGroup
{
    public int GroupId { get; set; }
    public string ValueType { get; set; } = "10-shot-series";
    public List<ShotPosition> Shots { get; set; } = [];
}

public class ShotsData
{
    public List<ShotPosition> WarmupShots { get; set; } = [];
    public List<ShotGroup> Groups { get; set; } = [];
}

public class TraineePerformanceDashboardViewModel
{
    public ClubMember TrainerMembership { get; set; } = null!;
    public ClubMember Trainee { get; set; } = null!;
    public IEnumerable<TrainingSession> TrainingSessions { get; set; } = [];
    public PerformanceStatistics Statistics { get; set; } = new();
    public string ChartDataJson { get; set; } = "{}";
    public string HeatMapDataJson { get; set; } = "{}";
    public int SelectedDays { get; set; } = 30;
}

public class TraineeSessionDetailsViewModel
{
    public TrainingSession Session { get; set; } = null!;
    public ClubMember TrainerMembership { get; set; } = null!;
    public ClubMember Trainee { get; set; } = null!;
}

public class EditClubViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool RequiresApproval { get; set; }
}
