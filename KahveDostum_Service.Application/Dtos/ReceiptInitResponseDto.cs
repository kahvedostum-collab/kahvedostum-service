namespace KahveDostum_Service.Application.Dtos;

public class ReceiptInitResponseDto
{
    public int ReceiptId { get; set; }
    public string ChannelKey { get; set; } = default!;
    public string Bucket { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public string UploadUrl { get; set; } = default!;
}