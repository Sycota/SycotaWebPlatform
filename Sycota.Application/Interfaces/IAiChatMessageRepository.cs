using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface IAiChatMessageRepository
{
    Task<IEnumerable<AiChatMessage>> GetBySessionIdAsync(int sessionId, int? limit = null);
    Task<AiChatMessage?> GetLastMessageAsync(int sessionId);
    Task AddAsync(AiChatMessage message);
    Task AddRangeAsync(IEnumerable<AiChatMessage> messages);
    Task DeleteBySessionIdAsync(int sessionId);
}
