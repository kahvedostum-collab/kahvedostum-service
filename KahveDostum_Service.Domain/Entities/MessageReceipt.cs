namespace KahveDostum_Service.Domain.Entities;

public enum MessageStatus
{
    Sent = 0,
    Delivered = 1,
    Seen = 2
}

public class MessageReceipt
{
    public int Id { get; set; }

    public int MessageId { get; set; }
    public int UserId { get; set; } // alıcı

    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime StatusAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = default!;
    public User User { get; set; } = default!;
}