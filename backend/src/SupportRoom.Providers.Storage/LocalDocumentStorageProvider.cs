using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SupportRoom.Providers.Storage;

/// <summary>
/// Writes to local disk under LOCAL_STORAGE_PATH (default "storage" relative to the working
/// directory) - real persistence across restarts, with no cloud account needed. Interim step
/// before HuaweiObs is wired up with real credentials; swapping DOCUMENT_STORAGE_PROVIDER later
/// needs no other code change.
/// </summary>
public sealed class LocalDocumentStorageProvider : IDocumentStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalDocumentStorageProvider> _logger;

    public LocalDocumentStorageProvider(ILogger<LocalDocumentStorageProvider> logger)
    {
        _logger = logger;
        // Keys already start with "documents/" (see DocumentResourceService's ObsKey format),
        // so the base path itself doesn't repeat that segment.
        var configured = Environment.GetEnvironmentVariable("LOCAL_STORAGE_PATH");
        _basePath = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "storage")
            : configured);
        Directory.CreateDirectory(_basePath);
    }

    public string BucketName => _basePath;

    /// <summary>ObsKey values are built server-side from an id plus the caller-supplied file
    /// name (see DocumentResourceService) - resolving through Path.GetFullPath and checking the
    /// result still lives under _basePath blocks a crafted file name (e.g. "../../secrets.txt")
    /// from escaping the storage directory - a real risk once writing to real disk.</summary>
    private string ResolvePath(string key)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, key));
        var basePathWithSeparator = _basePath.EndsWith(Path.DirectorySeparatorChar) ? _basePath : _basePath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(basePathWithSeparator, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Storage key \"{key}\" resolves outside the storage directory.");
        }
        return fullPath;
    }

    public Task UploadAsync(string key, Stream content, string contentType) => TimedCallAsync("upload", async () =>
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file);
        return true;
    });

    public Task<Stream> DownloadAsync(string key) => TimedCallAsync("download", () =>
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No local document stored for key \"{key}\".", path);
        }
        return Task.FromResult<Stream>(File.OpenRead(path));
    });

    public Task DeleteAsync(string key) => TimedCallAsync("delete", () =>
    {
        var path = ResolvePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.FromResult(true);
    });

    /// <summary>No real signed-URL concept for a local path - nothing currently consumes this
    /// (see IDocumentStorageProvider), so a stable non-HTTP identifier is enough for now.</summary>
    public Task<string> GetPresignedUrlAsync(string key) => Task.FromResult($"file://{ResolvePath(key)}");

    private async Task<T> TimedCallAsync<T>(string operation, Func<Task<T>> call)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await call();
            _logger.LogInformation("Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms", "local-storage", operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                "local-storage", operation, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
