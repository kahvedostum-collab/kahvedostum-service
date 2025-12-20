using System.Text.Json;

public record OcrResultMessage(
    string JobId,
    int ReceiptId,
    string Status,
    JsonElement? Payload,
    string? RawText,
    string? Error,
    string? ChannelKey 
);