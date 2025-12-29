namespace KahveDostum_Service.Infrastructure.Options;

public sealed class VeryfiOptions
{
    public string BaseUrl { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
}