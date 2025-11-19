namespace KahveDostum_Service.Domain.Entities;

public class ConversationParticipant
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public int UserId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = default!;
    public User User { get; set; } = default!;
}