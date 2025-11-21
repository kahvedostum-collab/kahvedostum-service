namespace KahveDostum_Service.Application.Dtos;

public class CafeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}