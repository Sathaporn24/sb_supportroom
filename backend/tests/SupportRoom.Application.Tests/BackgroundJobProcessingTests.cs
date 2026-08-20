using SupportRoom.Application.Services;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Application.Tests;

/// <summary>
/// DocumentIndexingResultMapper.Map and BackgroundJobBackoff.Calculate are pure functions pulled
/// out of BackgroundJobProcessor specifically so DI-5 (result -> status mapping) and DI-9 (retry
/// backoff) can be verified without a database, storage, or any provider (design.md R-12).
/// </summary>
public class BackgroundJobProcessingTests
{
    [Fact]
    public void Map_Success_ReturnsIndexedWithNoFailureReason()
    {
        var (status, reason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.Success);

        Assert.Equal(DocumentIndexingStatus.Indexed, status);
        Assert.Null(reason);
    }

    [Fact]
    public void Map_ExtractFailed_ReturnsFailedWithExtractFailedReason()
    {
        var (status, reason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.ExtractFailed);

        Assert.Equal(DocumentIndexingStatus.Failed, status);
        Assert.Equal(DocumentFailureReason.ExtractFailed, reason);
    }

    [Fact]
    public void Map_NoText_ReturnsFailedWithNoTextReason()
    {
        var (status, reason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.NoText);

        Assert.Equal(DocumentIndexingStatus.Failed, status);
        Assert.Equal(DocumentFailureReason.NoText, reason);
    }

    [Fact]
    public void Map_EmbeddingFailed_ReturnsFailedWithEmbeddingFailedReason()
    {
        var (status, reason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.EmbeddingFailed);

        Assert.Equal(DocumentIndexingStatus.Failed, status);
        Assert.Equal(DocumentFailureReason.EmbeddingFailed, reason);
    }

    [Fact]
    public void Map_IndexFailed_ReturnsFailedWithIndexFailedReason()
    {
        var (status, reason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.IndexFailed);

        Assert.Equal(DocumentIndexingStatus.Failed, status);
        Assert.Equal(DocumentFailureReason.IndexFailed, reason);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 5)]
    [InlineData(3, 15)]
    public void Calculate_ReturnsTheScheduledBackoffMinutes_ForEachAttemptCount(int attemptCount, int expectedMinutes)
    {
        var backoff = BackgroundJobBackoff.Calculate(attemptCount);

        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), backoff);
    }

    [Fact]
    public void MaxAttempts_IsThree()
    {
        Assert.Equal(3, BackgroundJobBackoff.MaxAttempts);
    }
}
