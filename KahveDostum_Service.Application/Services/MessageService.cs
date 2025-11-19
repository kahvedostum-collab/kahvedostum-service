using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class MessageService : IMessageService
{
    private readonly IUnitOfWork _uow;

    public MessageService(IUnitOfWork unitOfWork)
    {
        _uow = unitOfWork;
    }

    #region Helpers

    /// <summary>
    /// İki kullanıcı arkadaş değilse InvalidOperationException fırlatır.
    /// Tüm mesaj operasyonlarından önce çağrılır.
    /// </summary>
    private async Task EnsureFriendshipOrThrowAsync(int userId, int otherUserId)
    {
        if (userId == otherUserId)
            throw new InvalidOperationException("Kendinizle sohbet başlatamazsınız.");

        var areFriends = await _uow.Friendships.AreFriendsAsync(userId, otherUserId);
        if (!areFriends)
            throw new InvalidOperationException("Sadece arkadaş olduğunuz kullanıcılarla mesajlaşabilirsiniz.");
    }

    /// <summary>
    /// 1-1 sohbette, currentUser dışındaki katılımcının Id'sini döner.
    /// </summary>
    private int GetOtherParticipantUserId(Conversation conversation, int currentUserId)
    {
        var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != currentUserId);
        if (otherParticipant is null)
            throw new InvalidOperationException("Geçersiz sohbet katılımcıları.");

        return otherParticipant.UserId;
    }

    #endregion

    // --------------------------------------------------------------------------------
    // SEND
    // --------------------------------------------------------------------------------
    public async Task<MessageDto> SendMessageAsync(int currentUserId, SendMessageRequestDto request)
    {
        if (currentUserId == request.ToUserId)
            throw new InvalidOperationException("Kendinize mesaj gönderemezsiniz.");

        // 1) Arkadaşlık kontrolü
        await EnsureFriendshipOrThrowAsync(currentUserId, request.ToUserId);

        Conversation? conversation = null;

        // 2) Eğer client conversationId gönderiyorsa önce onu dene
        if (request.ConversationId.HasValue)
        {
            conversation = await _uow.Conversations.GetByIdAsync(request.ConversationId.Value);

            if (conversation != null)
            {
                // Bu sohbet gerçekten bu iki kullanıcıya mı ait?
                var participantIds = conversation.Participants.Select(p => p.UserId).ToList();

                // Eğer bu id'li sohbet başka kullanıcılara aitse, gelen conversationId'yi YOK SAY
                if (!participantIds.Contains(currentUserId) || !participantIds.Contains(request.ToUserId))
                {
                    conversation = null;
                }
            }
        }

        // 3) Hâlâ conversation yoksa, doğru 1-1 sohbeti bul/oluştur
        if (conversation is null)
        {
            conversation = await _uow.Conversations.EnsureDirectConversationAsync(
                currentUserId,
                request.ToUserId
            );
        }

        // 4) Mesaj oluştur
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = currentUserId,
            Content = request.Content,
            Type = MessageType.Text,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Messages.AddAsync(message);

        // 5) Alıcı için receipt (Sent)
        var receipt = new MessageReceipt
        {
            Message = message,
            UserId = request.ToUserId,
            Status = MessageStatus.Sent,
            StatusAt = DateTime.UtcNow
        };

        await _uow.MessageReceipts.AddAsync(receipt);
        await _uow.SaveChangesAsync();

        var sender = await _uow.Users.GetByIdAsync(currentUserId)
                     ?? throw new InvalidOperationException("Gönderen kullanıcı bulunamadı.");

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = conversation.Id,
            SenderId = sender.Id,
            SenderUserName = sender.UserName,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            StatusForCurrentUser = MessageStatus.Sent
        };
    }

    // --------------------------------------------------------------------------------
    // GET MESSAGES
    // --------------------------------------------------------------------------------
    public async Task<List<MessageDto>> GetMessagesAsync(
        int currentUserId,
        int conversationId,
        int take = 50,
        int skip = 0)
    {
        // Bu metot zaten hem sohbeti, hem katılımcıları, hem mesajları dolduruyor
        var convo = await _uow.Conversations.GetConversationWithMessagesAsync(
                        conversationId,
                        currentUserId,
                        take,
                        skip)
                    ?? throw new InvalidOperationException("Sohbet bulunamadı veya erişim yok.");

        // Diğer kullanıcıyı bul
        var otherUserId = GetOtherParticipantUserId(convo, currentUserId);

        // Arkadaşlık kontrolü
        await EnsureFriendshipOrThrowAsync(currentUserId, otherUserId);

        // Mesajları kronolojik sıraya göre map et
        return convo.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m =>
            {
                var receipt = m.Receipts.FirstOrDefault(r => r.UserId == currentUserId);
                var status = receipt?.Status ?? MessageStatus.Sent;

                return new MessageDto
                {
                    Id = m.Id,
                    ConversationId = convo.Id,
                    SenderId = m.SenderId,
                    SenderUserName = m.Sender?.UserName ?? string.Empty,
                    Content = m.IsDeleted ? "[deleted]" : m.Content,
                    CreatedAt = m.CreatedAt,
                    StatusForCurrentUser = status
                };
            })
            .ToList();
    }

    // --------------------------------------------------------------------------------
    // LIST CONVERSATIONS
    // --------------------------------------------------------------------------------
    public async Task<List<ConversationDto>> GetUserConversationsAsync(int currentUserId)
    {
        var convos = await _uow.Conversations.GetUserConversationsAsync(currentUserId);
        var list = new List<ConversationDto>();

        foreach (var c in convos)
        {
            // 1-1 senaryosu için diğer kullanıcıyı bul
            var otherUserId = GetOtherParticipantUserId(c, currentUserId);

            // Arkadaş değillerse bu sohbeti listeleme
            var areFriends = await _uow.Friendships.AreFriendsAsync(currentUserId, otherUserId);
            if (!areFriends)
                continue;

            var otherUser = await _uow.Users.GetByIdAsync(otherUserId)
                           ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

            var lastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

            list.Add(new ConversationDto
            {
                Id = c.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.UserName,
                OtherUserEmail = otherUser.Email,
                LastMessageAt = lastMessage?.CreatedAt ?? c.CreatedAt,
                LastMessagePreview = lastMessage?.Content,
                // İleride receipts üzerinden gerçek unread hesaplayabilirsin
                HasUnread = false
            });
        }

        return list;
    }

    // --------------------------------------------------------------------------------
    // MARK SEEN
    // --------------------------------------------------------------------------------
    public async Task MarkConversationSeenAsync(int currentUserId, int conversationId, int lastSeenMessageId)
    {
        // Bütün mesajları almak için büyük bir take veriyoruz
        var convo = await _uow.Conversations.GetConversationWithMessagesAsync(
                        conversationId,
                        currentUserId,
                        int.MaxValue,
                        0)
                    ?? throw new InvalidOperationException("Sohbet bulunamadı veya erişim yok.");

        var otherUserId = GetOtherParticipantUserId(convo, currentUserId);

        await EnsureFriendshipOrThrowAsync(currentUserId, otherUserId);

        var targetMessages = convo.Messages
            .Where(m => m.Id <= lastSeenMessageId)
            .ToList();

        foreach (var msg in targetMessages)
        {
            var receipt = await _uow.MessageReceipts.GetReceiptAsync(msg.Id, currentUserId);
            if (receipt is not null && receipt.Status < MessageStatus.Seen)
            {
                receipt.Status = MessageStatus.Seen;
                receipt.StatusAt = DateTime.UtcNow;
                _uow.MessageReceipts.Update(receipt);
            }
        }

        await _uow.SaveChangesAsync();
    }
}
