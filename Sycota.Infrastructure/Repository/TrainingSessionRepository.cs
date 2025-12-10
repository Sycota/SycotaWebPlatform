using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository
{
    public class TrainingSessionRepository : ITrainingSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public TrainingSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TrainingSession?> GetTrainingSessionByIdAsync(int trainingSessionId, TrainingSessionIncludeOptions include = TrainingSessionIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.TrainingSessions.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(ts => ts.Id == trainingSessionId);
        }

        public async Task<IEnumerable<TrainingSession>> GetAllTrainingSessionsAsync(TrainingSessionIncludeOptions include = TrainingSessionIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.TrainingSessions.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<TrainingSession>> GetAllTrainingSessionsByClubIdAsync(int clubId, TrainingSessionIncludeOptions include = TrainingSessionIncludeOptions.None)
        {
            var query = ApplyIncludeOptions(_context.TrainingSessions.AsQueryable(), include);

            return await query
                .AsNoTracking()
                .Where(ts => ts.ClubId == clubId)
                .ToListAsync();
        }

        public async Task AddTrainingSessionAsync(TrainingSession trainingSession)
        {
            await _context.TrainingSessions.AddAsync(trainingSession);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTrainingSessionAsync(TrainingSession trainingSession)
        {
            _context.TrainingSessions.Update(trainingSession);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTrainingSessionAsync(TrainingSession trainingSession)
        {
            _context.TrainingSessions.Remove(trainingSession);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTrainingSessionByIdAsync(int trainingSessionId)
        {
            var session = await _context.TrainingSessions.FindAsync(trainingSessionId);
            if (session is null) return;

            _context.TrainingSessions.Remove(session);
            await _context.SaveChangesAsync();
        }

        private static IQueryable<TrainingSession> ApplyIncludeOptions(IQueryable<TrainingSession> query, TrainingSessionIncludeOptions include)
        {
            if (include.HasFlag(TrainingSessionIncludeOptions.All) || include.HasFlag(TrainingSessionIncludeOptions.CreatedBy))
            {
                query = query.Include(ts => ts.CreatedBy);
            }

            if (include.HasFlag(TrainingSessionIncludeOptions.All) || include.HasFlag(TrainingSessionIncludeOptions.Club))
            {
                query = query.Include(ts => ts.Club);
            }

            return query;
        }
    }
}

