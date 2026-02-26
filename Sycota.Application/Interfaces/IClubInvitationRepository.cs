using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Application.Interfaces;

public interface IClubInvitationRepository
{
    Task<ClubInvitation?> GetByIdAsync(int id);
    Task<ClubInvitation?> GetByCodeAsync(string code);
    Task<IEnumerable<ClubInvitation>> GetByClubIdAsync(int clubId, MembershipRequestStatus? status = null);
    Task<IEnumerable<ClubInvitation>> GetByEmailAsync(string email, MembershipRequestStatus? status = null);
    Task<ClubInvitation?> GetPendingByEmailAndClubAsync(string email, int clubId);
    Task AddAsync(ClubInvitation invitation);
    Task UpdateAsync(ClubInvitation invitation);
    Task DeleteAsync(ClubInvitation invitation);
}
