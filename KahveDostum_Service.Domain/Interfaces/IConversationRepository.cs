using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetDirectConversationAsync(int userId1, int userId2);
    Task<Conversation> EnsureDirectConversationAsync(int userId1, int userId2);
    Task<List<Conversation>> GetUserConversationsAsync(int userId);
    Task<Conversation?> GetConversationWithMessagesAsync(int conversationId, int userId, int take, int skip);
}