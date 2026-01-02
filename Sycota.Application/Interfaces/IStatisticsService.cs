using Sycota.Domain.Classes;

namespace Sycota.Application.Interfaces;

public interface IStatisticsService
{
    Task<SessionStatistics?> ComputeSessionStatisticsAsync(int sessionResultId);
    Task<ShooterStatistics> ComputeShooterStatisticsAsync(int clubMemberId, DateTime? from = null, DateTime? to = null);
}