namespace KahveDostum_Service.Infrastructure.Options;

public class MinioOptions
{
    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = "receipts";
    public bool Secure { get; set; } = false;
    public int PresignExpirySeconds { get; set; } = 300;
}