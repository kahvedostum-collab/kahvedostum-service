namespace KahveDostum_Service.Application.Dtos;

public class OcrResultMessage
{
    public string JobId { get; set; } = default!;
    public long ReceiptId { get; set; }
    public string ChannelKey { get; set; } = default!;
    public string Status { get; set; } = default!; // DONE / FAILED
    public int Attempt { get; set; }

    public long Started { get; set; }
    public long Finished { get; set; }
    public long Elapsed => Finished - Started;

    public string Bucket { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;

    // Veryfi JSON cevabı – şimdilik dynamic/JsonElement
    public object? Payload { get; set; }
    public string? Error { get; set; }
}