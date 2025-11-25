namespace KahveDostum_Service.Domain.Entities
{
    public class Receipt
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? CafeId { get; set; }

        public string? Brand { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? ReceiptNo { get; set; }
        public string? Total { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? RawText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Cafe? Cafe { get; set; }

        public ICollection<ReceiptLine> Lines { get; set; } = new List<ReceiptLine>();
    }
}