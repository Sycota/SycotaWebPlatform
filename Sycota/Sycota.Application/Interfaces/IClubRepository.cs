using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces
{
    public interface IClubRepository
    {
        Task<Club> GetClubByIdAsync(int clubId);
        Task<IEnumerable<Club>> GetAllClubsAsync();
        Task AddClubAsync(Club club);
        Task UpdateClubAsync(Club club);
        Task DeleteClubAsync(Club club);

    }
}
