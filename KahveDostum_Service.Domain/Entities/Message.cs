namespace KahveDostum_Service.Domain.Entities;

public enum MessageType
{
    Text = 0,
    Image = 1,
    File = 2
}

public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public int SenderId { get; set; }

    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }

    public Conversation Conversation { get; set; } = default!;
    public User Sender { get; set; } = default!;

    public ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();
}