using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int currentUserId, SendMessageRequestDto request);
    Task<List<MessageDto>> GetMessagesAsync(int currentUserId, int conversationId, int take = 50, int skip = 0);
    Task<List<ConversationDto>> GetUserConversationsAsync(int currentUserId);
    Task MarkConversationSeenAsync(int currentUserId, int conversationId, int lastSeenMessageId);
}