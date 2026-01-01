using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces
{
    public interface IShooterProfileRepository
    {
        Task<ShooterProfile?> GetShooterProfileByIdAsync(int shooterProfileId, ShooterProfileIncludeOptions include = ShooterProfileIncludeOptions.None);
        Task<ShooterProfile?> GetShooterProfileByClubMemberIdAsync(int clubMemberId, ShooterProfileIncludeOptions include = ShooterProfileIncludeOptions.None);
        Task<IEnumerable<ShooterProfile>> GetAllShooterProfilesAsync(ShooterProfileIncludeOptions include = ShooterProfileIncludeOptions.None);
        Task AddShooterProfileAsync(ShooterProfile shooterProfile);
        Task UpdateShooterProfileAsync(ShooterProfile shooterProfile);
        Task DeleteShooterProfileAsync(ShooterProfile shooterProfile);
        Task DeleteShooterProfileByIdAsync(int shooterProfileId);
    }
}



