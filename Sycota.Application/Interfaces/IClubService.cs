using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Classes;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Application.Interfaces
{
    public interface IClubService
    {
        Task<ServiceResult<IEnumerable<ClubMember>>> GetClubMembersAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult<IEnumerable<ClubMember>>> GetTrainersAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult<IEnumerable<ClubMember>>> GetCompetitorsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult<IEnumerable<ClubMember>>> GetAdminsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult<IEnumerable<ClubMember>>> GetClubMembersForTrainerAsync(int trainerId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult<ClubMember>> GetClubMemberAsync(int clubMemberId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult> AddClubMemberAsync(string userId, int clubId, ClubRole role, int? trainerId = null);
        Task<ServiceResult> UpdateClubMemberAsync(ClubMember member);
        Task<ServiceResult> RemoveClubMemberAsync(int clubMemberId);
        Task<ServiceResult<bool>> UserMembershipExistsAsync(string userId, int clubId);
        Task<ServiceResult> AssignTrainerToCompetitorAsync(int competitorId, int? trainerId);
        Task<ServiceResult<IEnumerable<ClubMember>>> GetUnassignedCompetitorsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ServiceResult> SetAdminAsTrainerAsync(int adminMemberId, bool isAlsoTrainer);

        // Join Request methods
        Task<ServiceResult> CreateJoinRequestAsync(string userId, int clubId, ClubRole requestedRole, int? trainerId = null, string? message = null);
        Task<ServiceResult<IEnumerable<ClubJoinRequest>>> GetPendingJoinRequestsAsync(int clubId);
        Task<ServiceResult<ClubJoinRequest>> GetJoinRequestAsync(int requestId);
        Task<ServiceResult> ApproveJoinRequestAsync(int requestId, string adminUserId);
        Task<ServiceResult> RejectJoinRequestAsync(int requestId, string adminUserId, string? reason = null);
        Task<ServiceResult<bool>> HasPendingJoinRequestAsync(string userId, int clubId);

        // Invitation methods
        Task<ServiceResult> CreateInvitationAsync(int clubId, string email, ClubRole offeredRole, string createdById, int? trainerId = null, string? message = null, int expirationDays = 7);
        Task<ServiceResult<IEnumerable<ClubInvitation>>> GetPendingInvitationsAsync(int clubId);
        Task<ServiceResult<IEnumerable<ClubInvitation>>> GetPendingInvitationsForUserAsync(string email);
        Task<ServiceResult<ClubInvitation>> GetInvitationByCodeAsync(string code);
        Task<ServiceResult> AcceptInvitationAsync(string code, string userId);
        Task<ServiceResult> CancelInvitationAsync(int invitationId);
    }
}


