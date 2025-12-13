namespace KahveDostum_Service.Domain.Entities;

public class Company
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Cafe> Cafes { get; set; } = new List<Cafe>();
}