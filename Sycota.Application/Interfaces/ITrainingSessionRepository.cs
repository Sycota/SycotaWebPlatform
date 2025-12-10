using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces
{
    public interface ITrainingSessionRepository
    {
        Task<TrainingSession?> GetTrainingSessionByIdAsync(int trainingSessionId, TrainingSessionIncludeOptions include = TrainingSessionIncludeOptions.None);
        Task<IEnumerable<TrainingSession>> GetAllTrainingSessionsAsync(TrainingSessionIncludeOptions include = TrainingSessionIncludeOptions.None);
        Task<IEnumerable<TrainingSession>> GetAllTrainingSessionsByClubIdAsync(int clubId, TrainingSessionIncludeOptions include = TrainingSessionIncludeOptions.None);
        Task AddTrainingSessionAsync(TrainingSession trainingSession);
        Task UpdateTrainingSessionAsync(TrainingSession trainingSession);
        Task DeleteTrainingSessionAsync(TrainingSession trainingSession);
        Task DeleteTrainingSessionByIdAsync(int trainingSessionId);
    }
}

