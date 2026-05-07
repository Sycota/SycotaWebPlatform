using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class ClubAnnouncementRepository : IClubAnnouncementRepository
{
    private readonly ApplicationDbContext _context;

    public ClubAnnouncementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClubAnnouncement>> GetByClubIdAsync(int clubId)
    {
        return await _context.ClubAnnouncements
            .AsNoTracking()
            .Where(a => a.ClubId == clubId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(ClubAnnouncement announcement)
    {
        await _context.ClubAnnouncements.AddAsync(announcement);
        await _context.SaveChangesAsync();
    }
}
