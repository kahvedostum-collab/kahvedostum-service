using System.Security.Cryptography;
using System.Text;

namespace KahveDostum_Service.Application.Helpers;

public static class ReceiptHashHelper
{
    public static string Generate(
        string taxNumber,
        string total,
        DateTime receiptDate,
        string? receiptNo,
        string? rawText)
    {
        string raw;

        if (!string.IsNullOrWhiteSpace(receiptNo))
        {
            // 🥇 En güvenilir
            raw = $"{taxNumber}|{receiptNo}|{receiptDate:yyyyMMdd}";
        }
        else
        {
            // 🛟 Fallback
            var rawTextHash = string.IsNullOrWhiteSpace(rawText)
                ? "NO_TEXT"
                : Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(rawText)));

            raw = $"{taxNumber}|{total}|{receiptDate:yyyyMMddHHmm}|{rawTextHash}";
        }

        using var sha = SHA256.Create();
        return Convert.ToHexString(
            sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }

}