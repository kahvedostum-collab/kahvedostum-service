using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class ConversationRepository(AppDbContext context)
    : Repository<Conversation>(context), IConversationRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Conversation?> GetDirectConversationAsync(int userId1, int userId2)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                c.Participants.Any(p => p.UserId == userId1) &&
                c.Participants.Any(p => p.UserId == userId2) &&
                c.Participants.Count == 2);
    }

    public async Task<Conversation> EnsureDirectConversationAsync(int userId1, int userId2)
    {
        var existing = await GetDirectConversationAsync(userId1, userId2);
        if (existing is not null)
            return existing;

        var conversation = new Conversation();
        await _context.Conversations.AddAsync(conversation);

        var p1 = new ConversationParticipant { Conversation = conversation, UserId = userId1 };
        var p2 = new ConversationParticipant { Conversation = conversation, UserId = userId2 };

        await _context.ConversationParticipants.AddRangeAsync(p1, p2);

        return conversation; // SaveChangesAsync UoW tarafından çağrılacak
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(int userId)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.CreatedAt) ?? c.CreatedAt)
            .ToListAsync();
    }
    
    public async Task AddParticipantAsync(int conversationId, int userId)
    {
        var exists = await _context.ConversationParticipants
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId);

        if (!exists)
        {
            _context.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
        }
    }

    public async Task<Conversation?> GetConversationWithMessagesAsync(int conversationId, int userId, int take, int skip)
    {
        // sadece katılımcı olduğu sohbetlere erişebilsin
        var query = _context.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Take(take))
                .ThenInclude(m => m.Sender)
            .Where(c => c.Id == conversationId && c.Participants.Any(p => p.UserId == userId));

        return await query.FirstOrDefaultAsync();
    }
}
