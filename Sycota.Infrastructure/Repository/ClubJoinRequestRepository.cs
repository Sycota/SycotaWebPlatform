using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class ClubJoinRequestRepository : IClubJoinRequestRepository
{
    private readonly ApplicationDbContext _context;

    public ClubJoinRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ClubJoinRequest?> GetByIdAsync(int id)
    {
        return await _context.ClubJoinRequests
            .Include(r => r.User)
            .Include(r => r.Club)
            .Include(r => r.RequestedTrainer)
                .ThenInclude(t => t!.User)
            .Include(r => r.ProcessedBy)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<ClubJoinRequest>> GetByClubIdAsync(int clubId, MembershipRequestStatus? status = null)
    {
        var query = _context.ClubJoinRequests
            .Include(r => r.User)
            .Include(r => r.RequestedTrainer)
                .ThenInclude(t => t!.User)
            .Where(r => r.ClubId == clubId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
    }

    public async Task<IEnumerable<ClubJoinRequest>> GetByUserIdAsync(string userId)
    {
        return await _context.ClubJoinRequests
            .Include(r => r.Club)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<ClubJoinRequest?> GetPendingByUserAndClubAsync(string userId, int clubId)
    {
        return await _context.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.UserId == userId 
                && r.ClubId == clubId 
                && r.Status == MembershipRequestStatus.Pending);
    }

    public async Task AddAsync(ClubJoinRequest request)
    {
        await _context.ClubJoinRequests.AddAsync(request);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClubJoinRequest request)
    {
        _context.ClubJoinRequests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ClubJoinRequest request)
    {
        _context.ClubJoinRequests.Remove(request);
        await _context.SaveChangesAsync();
    }
}
