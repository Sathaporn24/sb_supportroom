namespace SupportRoom.Providers.Storage;

public interface IDocumentStorageProvider
{
    /// <summary>Reported back to the caller so DocumentResource.ObsBucket reflects where the
    /// file actually landed, without the Application layer needing to know provider-specific config.</summary>
    string BucketName { get; }

    Task UploadAsync(string key, Stream content, string contentType);
    Task<Stream> DownloadAsync(string key);
    Task DeleteAsync(string key);

    /// <summary>Time-limited URL for CS to preview/download the file directly - not stored, generated on demand.</summary>
    Task<string> GetPresignedUrlAsync(string key);
}
