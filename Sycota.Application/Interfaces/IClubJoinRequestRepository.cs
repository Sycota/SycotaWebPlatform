using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Application.Interfaces;

public interface IClubJoinRequestRepository
{
    Task<ClubJoinRequest?> GetByIdAsync(int id);
    Task<IEnumerable<ClubJoinRequest>> GetByClubIdAsync(int clubId, MembershipRequestStatus? status = null);
    Task<IEnumerable<ClubJoinRequest>> GetByUserIdAsync(string userId);
    Task<ClubJoinRequest?> GetPendingByUserAndClubAsync(string userId, int clubId);
    Task AddAsync(ClubJoinRequest request);
    Task UpdateAsync(ClubJoinRequest request);
    Task DeleteAsync(ClubJoinRequest request);
}
