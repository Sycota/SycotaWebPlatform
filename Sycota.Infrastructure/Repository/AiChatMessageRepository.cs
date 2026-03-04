using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class AiChatMessageRepository : IAiChatMessageRepository
{
    private readonly ApplicationDbContext _context;

    public AiChatMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AiChatMessage>> GetBySessionIdAsync(int sessionId, int? limit = null)
    {
        var query = _context.AiChatMessages
            .Where(m => m.TrainingSessionId == sessionId)
            .OrderBy(m => m.CreatedAt);

        if (limit.HasValue)
        {
            // Get the most recent messages up to the limit
            return await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit.Value)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        return await query.ToListAsync();
    }

    public async Task<AiChatMessage?> GetLastMessageAsync(int sessionId)
    {
        return await _context.AiChatMessages
            .Where(m => m.TrainingSessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(AiChatMessage message)
    {
        await _context.AiChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<AiChatMessage> messages)
    {
        await _context.AiChatMessages.AddRangeAsync(messages);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBySessionIdAsync(int sessionId)
    {
        var messages = await _context.AiChatMessages
            .Where(m => m.TrainingSessionId == sessionId)
            .ToListAsync();

        _context.AiChatMessages.RemoveRange(messages);
        await _context.SaveChangesAsync();
    }
}
