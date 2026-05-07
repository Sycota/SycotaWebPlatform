using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface IClubAnnouncementRepository
{
    Task<IEnumerable<ClubAnnouncement>> GetByClubIdAsync(int clubId);
    Task AddAsync(ClubAnnouncement announcement);
}
