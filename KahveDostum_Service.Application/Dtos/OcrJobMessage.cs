namespace KahveDostum_Service.Application.Dtos;

public class OcrJobMessage
{
    public string JobId { get; set; } = default!;
    public int ReceiptId { get; set; }
    public int UserId { get; set; }
    public int? CafeId { get; set; }
    public string Bucket { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public double? ClientLat { get; set; }
    public double? ClientLng { get; set; }
    public string ChannelKey { get; set; } = default!;
}