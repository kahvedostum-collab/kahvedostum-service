using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace KahveDostum_Service.Infrastructure.Services;

public sealed class MinioStorage : IObjectStorage
{
    private readonly MinioClient? _minio;

    public MinioStorage(IOptions<MinioOptions> opt)
    {
        var o = opt.Value;

        _minio = (MinioClient?)new MinioClient()
            .WithEndpoint(o.Endpoint)
            .WithCredentials(o.AccessKey, o.SecretKey)
            .WithSSL(o.Secure)
            .Build();
    }

    public async Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket),
            cancellationToken: ct);

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucket),
                cancellationToken: ct);
        }
    }

    public async Task<string> PresignPutAsync(string bucket, string objectKey, int expirySeconds, CancellationToken ct = default)
    {
        // Presign çağrısı ct almaz; ama bucket check/create'i ct ile yaptık.
        return await _minio.PresignedPutObjectAsync(
            new PresignedPutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry(expirySeconds));
    }
}