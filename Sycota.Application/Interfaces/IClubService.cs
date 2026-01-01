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
    }
}


