using System.Text.Json;

namespace KahveDostum_Service.Application.Dtos;

public record OcrResultMessage(
    string JobId,
    Guid ReceiptId,
    string Status,   
    JsonElement? Payload,
    string? RawText,
    string? Error
);