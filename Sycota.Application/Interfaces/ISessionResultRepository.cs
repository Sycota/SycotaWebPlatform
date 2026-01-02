using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface ISessionResultRepository
{
    Task<SessionResult?> GetSessionResultByIdAsync(int sessionResultId, SessionResultIncludeOptions include = SessionResultIncludeOptions.None);
    Task<IEnumerable<SessionResult>> GetAllSessionResultsByClubMemberIdAsync(int clubMemberId, DateTime? from = null, DateTime? to = null, SessionResultIncludeOptions include = SessionResultIncludeOptions.None);
    Task<IEnumerable<SessionResult>> GetAllSessionResultsByTrainingSessionIdAsync(int trainingSessionId, SessionResultIncludeOptions include = SessionResultIncludeOptions.None);
    Task<IEnumerable<SessionResult>> GetAllSessionResultsAsync(SessionResultIncludeOptions include = SessionResultIncludeOptions.None);

    Task AddSessionResultAsync(SessionResult sessionResult);
    Task UpdateSessionResultAsync(SessionResult sessionResult);
    Task DeleteSessionResultAsync(SessionResult sessionResult);
    Task DeleteSessionResultByIdAsync(int sessionResultId);
}