namespace KahveDostum_Service.Domain.Entities;

public class Cafe
{
    public int Id { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = default!;

    // FİŞTEN GELEN ANA EŞLEŞME ALANI
    public string? TaxNumber { get; set; }   // VKN

    // FALLBACK EŞLEŞME
    public string Address { get; set; } = default!;
    public string NormalizedAddress { get; set; } = default!;

    public string? Description { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}