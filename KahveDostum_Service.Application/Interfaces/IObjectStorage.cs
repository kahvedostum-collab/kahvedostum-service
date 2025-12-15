namespace KahveDostum_Service.Application.Interfaces;

public interface IObjectStorage
{
    Task EnsureBucketAsync(string bucket, CancellationToken ct = default);
    Task<string> PresignPutAsync(string bucket, string objectKey, int expirySeconds, CancellationToken ct = default);
}