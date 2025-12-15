namespace KahveDostum_Service.Application.Dtos;

public class ReceiptInitRequestDto
{
    public int? CafeId { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}