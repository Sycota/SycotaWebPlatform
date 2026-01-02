using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class ShotRepository : IShotRepository
{
    private readonly ApplicationDbContext _context;

    public ShotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Shot?> GetShotByIdAsync(int shotId, ShotIncludeOptions include = ShotIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.Shots.AsQueryable(), include);
        return await query.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shotId);
    }

    public async Task<IEnumerable<Shot>> GetAllShotsBySessionResultIdAsync(int sessionResultId, ShotIncludeOptions include = ShotIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.Shots.AsQueryable(), include);
        return await query.AsNoTracking().Where(s => s.SessionResultId == sessionResultId).ToListAsync();
    }

    public async Task<IEnumerable<Shot>> GetAllShotsByClubMemberIdAsync(int clubMemberId, DateTime? from = null, DateTime? to = null, ShotIncludeOptions include = ShotIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.Shots.AsQueryable(), include);

        // join via SessionResults to filter by ClubMemberId
        query = query.Where(s => s.SessionResult.ClubMemberId == clubMemberId);

        if (from.HasValue)
            query = query.Where(s => s.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.Timestamp <= to.Value);

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task AddShotAsync(Shot shot)
    {
        await _context.Shots.AddAsync(shot);
        await _context.SaveChangesAsync();
    }

    public async Task AddShotsAsync(IEnumerable<Shot> shots)
    {
        await _context.Shots.AddRangeAsync(shots);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateShotAsync(Shot shot)
    {
        _context.Shots.Update(shot);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteShotAsync(Shot shot)
    {
        _context.Shots.Remove(shot);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteShotsBySessionResultIdAsync(int sessionResultId)
    {
        var shots = _context.Shots.Where(s => s.SessionResultId == sessionResultId);
        _context.Shots.RemoveRange(shots);
        await _context.SaveChangesAsync();
    }

    private static IQueryable<Shot> ApplyIncludeOptions(IQueryable<Shot> query, ShotIncludeOptions include)
    {
        if (include.HasFlag(ShotIncludeOptions.All) || include.HasFlag(ShotIncludeOptions.SessionResult))
        {
            query = query.Include(s => s.SessionResult);
        }

        return query;
    }
}