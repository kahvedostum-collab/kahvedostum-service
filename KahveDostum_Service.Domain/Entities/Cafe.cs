namespace KahveDostum_Service.Domain.Entities;

public class Cafe
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Description { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}