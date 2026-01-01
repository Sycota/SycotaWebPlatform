using Sycota.Domain.Entities;
using Sycota.Application.Interfaces.Options;

namespace Sycota.Application.Interfaces
{
    public interface IClubMemberRepository
    {
        Task<ClubMember?> GetClubMemberByIdAsync(int clubMemberId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<IEnumerable<ClubMember>> GetAllClubMembersByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<IEnumerable<ClubMember>> GetAllClubMembersAsync(ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<IEnumerable<ClubMember>> GetAllTrainersByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<IEnumerable<ClubMember>> GetAllCompetitorsByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<IEnumerable<ClubMember>> GetAllAdminsByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<IEnumerable<ClubMember>> GetCompetitorsByTrainerIdAsync(int trainerId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task<ClubMember?> GetByUserAndClubAsync(string userId, int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None);
        Task AddClubMemberAsync(ClubMember clubMember);
        Task UpdateClubMemberAsync(ClubMember clubMember);
        Task DeleteClubMemberAsync(ClubMember clubMember);
        Task DeleteClubMemberByIdAsync(int clubMemberId);
    }
}
