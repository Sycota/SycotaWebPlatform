using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class ClubInvitationRepository : IClubInvitationRepository
{
    private readonly ApplicationDbContext _context;

    public ClubInvitationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ClubInvitation?> GetByIdAsync(int id)
    {
        return await _context.ClubInvitations
            .Include(i => i.Club)
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTrainer)
                .ThenInclude(t => t!.User)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<ClubInvitation?> GetByCodeAsync(string code)
    {
        return await _context.ClubInvitations
            .Include(i => i.Club)
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTrainer)
                .ThenInclude(t => t!.User)
            .FirstOrDefaultAsync(i => i.InvitationCode == code);
    }

    public async Task<IEnumerable<ClubInvitation>> GetByClubIdAsync(int clubId, MembershipRequestStatus? status = null)
    {
        var query = _context.ClubInvitations
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTrainer)
                .ThenInclude(t => t!.User)
            .Include(i => i.AcceptedByUser)
            .Where(i => i.ClubId == clubId);

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<ClubInvitation>> GetByEmailAsync(string email, MembershipRequestStatus? status = null)
    {
        var query = _context.ClubInvitations
            .Include(i => i.Club)
            .Include(i => i.CreatedBy)
            .Include(i => i.AssignedTrainer)
                .ThenInclude(t => t!.User)
            .Where(i => i.Email.ToLower() == email.ToLower() && i.ExpiresAt > DateTime.UtcNow);

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task<ClubInvitation?> GetPendingByEmailAndClubAsync(string email, int clubId)
    {
        return await _context.ClubInvitations
            .FirstOrDefaultAsync(i => i.Email.ToLower() == email.ToLower() 
                && i.ClubId == clubId 
                && i.Status == MembershipRequestStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow);
    }

    public async Task AddAsync(ClubInvitation invitation)
    {
        await _context.ClubInvitations.AddAsync(invitation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClubInvitation invitation)
    {
        _context.ClubInvitations.Update(invitation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ClubInvitation invitation)
    {
        _context.ClubInvitations.Remove(invitation);
        await _context.SaveChangesAsync();
    }
}
