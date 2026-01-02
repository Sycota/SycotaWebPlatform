using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class SessionResultRepository : ISessionResultRepository
{
    private readonly ApplicationDbContext _context;

    public SessionResultRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SessionResult?> GetSessionResultByIdAsync(int sessionResultId, SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.SessionResults.AsQueryable(), include);
        return await query.AsNoTracking().FirstOrDefaultAsync(sr => sr.Id == sessionResultId);
    }

    public async Task<IEnumerable<SessionResult>> GetAllSessionResultsByClubMemberIdAsync(int clubMemberId, DateTime? from = null, DateTime? to = null, SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.SessionResults.AsQueryable(), include);

        if (from.HasValue)
            query = query.Where(sr => sr.SessionDate >= from.Value);
        if (to.HasValue)
            query = query.Where(sr => sr.SessionDate <= to.Value);

        return await query.AsNoTracking().Where(sr => sr.ClubMemberId == clubMemberId).ToListAsync();
    }

    public async Task<IEnumerable<SessionResult>> GetAllSessionResultsByTrainingSessionIdAsync(int trainingSessionId, SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.SessionResults.AsQueryable(), include);
        return await query.AsNoTracking().Where(sr => sr.TrainingSessionId == trainingSessionId).ToListAsync();
    }

    public async Task<IEnumerable<SessionResult>> GetAllSessionResultsAsync(SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
    {
        var query = ApplyIncludeOptions(_context.SessionResults.AsQueryable(), include);
        return await query.AsNoTracking().ToListAsync();
    }

    public async Task AddSessionResultAsync(SessionResult sessionResult)
    {
        await _context.SessionResults.AddAsync(sessionResult);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSessionResultAsync(SessionResult sessionResult)
    {
        _context.SessionResults.Update(sessionResult);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSessionResultAsync(SessionResult sessionResult)
    {
        _context.SessionResults.Remove(sessionResult);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSessionResultByIdAsync(int sessionResultId)
    {
        var entity = await _context.SessionResults.FindAsync(sessionResultId);
        if (entity is null) return;
        _context.SessionResults.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private static IQueryable<SessionResult> ApplyIncludeOptions(IQueryable<SessionResult> query, SessionResultIncludeOptions include)
    {
        if (include.HasFlag(SessionResultIncludeOptions.All) || include.HasFlag(SessionResultIncludeOptions.Shots))
        {
            query = query.Include(sr => sr.Shots);
        }

        if (include.HasFlag(SessionResultIncludeOptions.All) || include.HasFlag(SessionResultIncludeOptions.ClubMember))
        {
            query = query.Include(sr => sr.ClubMember).ThenInclude(cm => cm.User);
        }

        if (include.HasFlag(SessionResultIncludeOptions.All) || include.HasFlag(SessionResultIncludeOptions.TrainingSession))
        {
            query = query.Include(sr => sr.TrainingSession);
        }

        return query;
    }
}