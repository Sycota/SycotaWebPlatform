using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository
{
    public class ShooterProfileRepository : IShooterProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public ShooterProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShooterProfile?> GetShooterProfileByIdAsync(int shooterProfileId, ShooterProfileIncludeOptions include = ShooterProfileIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ShooterProfiles.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.Id == shooterProfileId);
        }

        public async Task<ShooterProfile?> GetShooterProfileByClubMemberIdAsync(int clubMemberId, ShooterProfileIncludeOptions include = ShooterProfileIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ShooterProfiles.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.ClubMemberId == clubMemberId);
        }

        public async Task<IEnumerable<ShooterProfile>> GetAllShooterProfilesAsync(ShooterProfileIncludeOptions include = ShooterProfileIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.ShooterProfiles.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddShooterProfileAsync(ShooterProfile shooterProfile)
        {
            await _context.ShooterProfiles.AddAsync(shooterProfile);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateShooterProfileAsync(ShooterProfile shooterProfile)
        {
            _context.ShooterProfiles.Update(shooterProfile);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteShooterProfileAsync(ShooterProfile shooterProfile)
        {
            _context.ShooterProfiles.Remove(shooterProfile);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteShooterProfileByIdAsync(int shooterProfileId)
        {
            var profile = await _context.ShooterProfiles.FindAsync(shooterProfileId);
            if (profile is null) return;

            _context.ShooterProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }

        private static IQueryable<ShooterProfile> ApplyIncludeOptions(IQueryable<ShooterProfile> query, ShooterProfileIncludeOptions include)
        {
            var includeClubMember = include.HasFlag(ShooterProfileIncludeOptions.All) ||
                                    include.HasFlag(ShooterProfileIncludeOptions.ClubMember) ||
                                    include.HasFlag(ShooterProfileIncludeOptions.ClubMemberUser) ||
                                    include.HasFlag(ShooterProfileIncludeOptions.ClubMemberTrainer);

            if (includeClubMember)
            {
                query = query.Include(sp => sp.ClubMember);
            }

            if (include.HasFlag(ShooterProfileIncludeOptions.All) || include.HasFlag(ShooterProfileIncludeOptions.ClubMemberUser))
            {
                query = query.Include(sp => sp.ClubMember).ThenInclude(cm => cm.User);
            }

            if (include.HasFlag(ShooterProfileIncludeOptions.All) || include.HasFlag(ShooterProfileIncludeOptions.ClubMemberTrainer))
            {
                query = query.Include(sp => sp.ClubMember)
                             .ThenInclude(cm => cm.Trainer)
                             .ThenInclude(t => t!.User);
            }

            return query;
        }
    }
}

