using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces
{
    public interface IClubRepository
    {
        Task<Club?> GetClubByIdAsync(int clubId, ClubIncludeOptions include = ClubIncludeOptions.None);
        Task<IEnumerable<Club>> GetAllClubsAsync(ClubIncludeOptions include = ClubIncludeOptions.None);
        Task AddClubAsync(Club club);
        Task UpdateClubAsync(Club club);
        Task DeleteClubAsync(Club club);
        Task DeleteClubByIdAsync(int clubId);
    }
}
