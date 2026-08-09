using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Storage;

/// <summary>
/// Huawei OBS speaks the S3 protocol, so this reuses AWSSDK.S3 pointed at OBS's own endpoint
/// instead of AWS - ForcePathStyle is required because OBS (like most non-AWS S3-compatible
/// services) doesn't support AWS's virtual-hosted-style bucket addressing. Credentials come from
/// ExternalServiceEnv.GetHuaweiObs() - see docs on DOCUMENT_STORAGE_PROVIDER=huawei-obs setup.
/// </summary>
public sealed class HuaweiObsDocumentStorageProvider(ILogger<HuaweiObsDocumentStorageProvider> logger) : IDocumentStorageProvider
{
    public string BucketName => ExternalServiceEnv.GetHuaweiObs().Bucket;

    private AmazonS3Client CreateClient()
    {
        var creds = ExternalServiceEnv.GetHuaweiObs();
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{creds.Endpoint}",
            AuthenticationRegion = creds.Region,
            ForcePathStyle = true,
        };
        return new AmazonS3Client(creds.AccessKey, creds.SecretKey, config);
    }

    public async Task UploadAsync(string key, Stream content, string contentType)
    {
        await TimedCallAsync("upload", async () =>
        {
            using var client = CreateClient();
            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
            });
            return true;
        });
    }

    public Task<Stream> DownloadAsync(string key) => TimedCallAsync("download", async () =>
    {
        using var client = CreateClient();
        var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = BucketName, Key = key });
        // Copy out of the response's stream before the client (and its underlying HttpClient
        // response) is disposed at the end of this using block - the caller owns the returned
        // stream and may read it well after this method returns.
        var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer);
        buffer.Position = 0;
        return (Stream)buffer;
    });

    public async Task DeleteAsync(string key)
    {
        await TimedCallAsync("delete", async () =>
        {
            using var client = CreateClient();
            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = BucketName, Key = key });
            return true;
        });
    }

    public Task<string> GetPresignedUrlAsync(string key) => TimedCallAsync("presign", async () =>
    {
        using var client = CreateClient();
        return await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(15),
        });
    });

    private async Task<T> TimedCallAsync<T>(string operation, Func<Task<T>> call)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await call();
            logger.LogInformation("Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms", "huawei-obs", operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                "huawei-obs", operation, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
