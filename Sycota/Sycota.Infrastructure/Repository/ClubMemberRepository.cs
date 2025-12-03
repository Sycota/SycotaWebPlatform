using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository
{
    public class ClubMemberRepository : IClubMemberRepository
    {
        private readonly ApplicationDbContext _context;

        public ClubMemberRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ClubMember?> GetClubMemberByIdAsync(int clubMemberId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ClubMembers.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(cm => cm.Id == clubMemberId);
        }

        public async Task<IEnumerable<ClubMember>> GetAllClubMembersByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ClubMembers.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .Where(cm => cm.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClubMember>> GetAllTrainersByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ClubMembers.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .Where(cm => cm.ClubId == clubId)
                .Where(cm => cm.Role == ClubRole.Trainer)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClubMember>> GetAllCompetitorsByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ClubMembers.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .Where(cm => cm.ClubId == clubId)
                .Where(cm => cm.Role == ClubRole.Competitor)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClubMember>> GetAllAdminsByClubIdAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ClubMembers.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .Where(cm => cm.ClubId == clubId)
                .Where(cm => cm.Role == ClubRole.Admin)
                .ToListAsync();
        }
        public async Task<IEnumerable<ClubMember>> GetAllClubMembersAsync(ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ClubMembers.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddClubMemberAsync(ClubMember clubMember)
        {
            await _context.ClubMembers.AddAsync(clubMember);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClubMemberAsync(ClubMember clubMember)
        {
            _context.ClubMembers.Update(clubMember);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClubMemberAsync(ClubMember clubMember)
        {
            _context.ClubMembers.Remove(clubMember);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClubMemberByIdAsync(int clubMemberId)
        {
            var member = await _context.ClubMembers.FindAsync(clubMemberId);
            if (member is null) return;

            _context.ClubMembers.Remove(member);
            await _context.SaveChangesAsync();
        }

        private static IQueryable<ClubMember> ApplyIncludeOptions(IQueryable<ClubMember> query, ClubMemberIncludeOptions include)
        {
            if (include.HasFlag(ClubMemberIncludeOptions.All) || include.HasFlag(ClubMemberIncludeOptions.User))
            {
                query = query.Include(cm => cm.User);
            }

            if (include.HasFlag(ClubMemberIncludeOptions.All) || include.HasFlag(ClubMemberIncludeOptions.Club))
            {
                query = query.Include(cm => cm.Club);
            }

            if (include.HasFlag(ClubMemberIncludeOptions.All) || include.HasFlag(ClubMemberIncludeOptions.Trainer))
            {
                query = query.Include(cm => cm.Trainer);
            }

            if (include.HasFlag(ClubMemberIncludeOptions.All) || include.HasFlag(ClubMemberIncludeOptions.Competitors))
            {
                query = query.Include(cm => cm.Competitors);
            }

            if (include.HasFlag(ClubMemberIncludeOptions.All) || include.HasFlag(ClubMemberIncludeOptions.ShooterProfile))
            {
                query = query.Include(cm => cm.ShooterProfile);
            }

            return query;
        }
    }
}
