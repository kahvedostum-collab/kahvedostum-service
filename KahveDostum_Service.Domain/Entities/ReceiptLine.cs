namespace KahveDostum_Service.Domain.Entities
{
    public class ReceiptLine
    {
        public int Id { get; set; }
        public int ReceiptId { get; set; }

        public int LineIndex { get; set; }
        public string Text { get; set; }

        public string? PredictedLabel { get; set; }
        public string? TrueLabel { get; set; }

        public Receipt Receipt { get; set; }
    }
}