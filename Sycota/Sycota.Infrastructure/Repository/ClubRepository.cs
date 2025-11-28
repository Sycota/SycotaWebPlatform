using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository
{
    public class ClubRepository : IClubRepository
    {
        private readonly ApplicationDbContext _context;

        public ClubRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Club?> GetClubByIdAsync(int clubId, ClubIncludeOptions include = ClubIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.Clubs.AsQueryable(), include);
            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clubId);
        }

        public async Task<IEnumerable<Club>> GetAllClubsAsync(ClubIncludeOptions include = ClubIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.Clubs.AsQueryable(), include);
            return await query
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddClubAsync(Club club)
        {
            await _context.Clubs.AddAsync(club);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClubAsync(Club club)
        {
            _context.Clubs.Update(club);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClubAsync(Club club)
        {
            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClubByIdAsync(int clubId)
        {
            var club = await _context.Clubs.FindAsync(clubId);
            if (club is null) return;

            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();
        }

        private static IQueryable<Club> ApplyIncludeOptions(IQueryable<Club> query, ClubIncludeOptions include)
        {
            if (include.HasFlag(ClubIncludeOptions.All) || include.HasFlag(ClubIncludeOptions.CreatedBy))
            {
                query = query.Include(c => c.CreatedBy);
            }

            if (include.HasFlag(ClubIncludeOptions.All) || include.HasFlag(ClubIncludeOptions.Members))
            {
                query = query.Include(c => c.Members);
            }

            if (include.HasFlag(ClubIncludeOptions.All) || include.HasFlag(ClubIncludeOptions.TrainingSessions))
            {
                query = query.Include(c => c.TrainingSessions);
            }

            return query;
        }
    }
}