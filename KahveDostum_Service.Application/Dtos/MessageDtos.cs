using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Application.Dtos;

public class SendMessageRequestDto
{
    public int ToUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? ConversationId { get; set; } // bilinmiyorsa null
}

public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public MessageStatus StatusForCurrentUser { get; set; }
}

public class ConversationDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserEmail { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public bool HasUnread { get; set; }
}

public class MarkSeenRequestDto
{
    public int ConversationId { get; set; }
    public int LastSeenMessageId { get; set; }
}