namespace KahveDostum_Service.Infrastructure.Options;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = default!;
    public bool Secure { get; set; }          // appsettings: "Secure": false
    public int PresignExpirySeconds { get; set; }
}