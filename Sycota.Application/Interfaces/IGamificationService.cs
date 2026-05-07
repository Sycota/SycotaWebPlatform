using Sycota.Domain.Classes;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface IGamificationService
{
    GamificationProgress Calculate(IEnumerable<TrainingSession> sessions, ShooterProfile? shooterProfile, DateTime? nowUtc = null);
}
