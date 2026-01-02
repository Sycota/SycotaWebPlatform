using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface IShotRepository
{
    Task<Shot?> GetShotByIdAsync(int shotId, ShotIncludeOptions include = ShotIncludeOptions.None);
    Task<IEnumerable<Shot>> GetAllShotsBySessionResultIdAsync(int sessionResultId, ShotIncludeOptions include = ShotIncludeOptions.None);
    Task<IEnumerable<Shot>> GetAllShotsByClubMemberIdAsync(int clubMemberId, DateTime? from = null, DateTime? to = null, ShotIncludeOptions include = ShotIncludeOptions.None);
    Task AddShotAsync(Shot shot);
    Task AddShotsAsync(IEnumerable<Shot> shots);
    Task UpdateShotAsync(Shot shot);
    Task DeleteShotAsync(Shot shot);
    Task DeleteShotsBySessionResultIdAsync(int sessionResultId);
}