using System.ComponentModel.DataAnnotations;

namespace KahveDostum_Service.Application.Dtos;

public class CafeTokenDto
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

public class ActivateSessionByTokenRequestDto
{
    public string Token { get; set; } = default!;
}

public class CreateReceiptDto
{
    // OPSİYONEL
    public string? Brand { get; set; }
    public string? RawText { get; set; }
    public string? ReceiptNo { get; set; }

    // ZORUNLU
    [Required]
    public string TaxNumber { get; set; } = default!;

    [Required]
    public string Address { get; set; } = default!;

    [Required]
    public string Total { get; set; } = default!;

    [Required]
    public DateTime ReceiptDate { get; set; }
}