using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Tts;

using static SupportRoom.Application.Tests.TestFixtures;

namespace SupportRoom.Application.Tests;

public class SlidesServiceTests
{
    private readonly SlidesService _service = new(
        new FakeUnitOfWork(), new FakeServiceProvider(), NullLogger<ISlidesService>.Instance,
        new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance));

    [Fact]
    public async Task ResolveAsync_ReturnsTheRealPresentationId()
    {
        var result = await _service.ResolveAsync(new ResolveSlidesDto { SlidesSourceUrl = TestGoogleSlidesUrl });
        Assert.Equal(GooglePresentationId, result.PresentationId);
    }

    [Fact]
    public async Task GetContentAsync_ReturnsTheRealDeck()
    {
        var content = await _service.GetContentAsync(GooglePresentationId);
        Assert.NotEmpty(content.Slides);
    }
}

public class TtsServiceTests
{
    private readonly TtsService _service = new(
        new FakeUnitOfWork(), new FakeServiceProvider(), NullLogger<ITtsService>.Instance,
        new EdgeTtsProvider(NullLogger<EdgeTtsProvider>.Instance));

    [Fact]
    public async Task SynthesizeAsync_ReturnsPlayableAudioBytes()
    {
        var result = await _service.SynthesizeAsync(new SynthesizeSpeechDto { Text = "สวัสดีค่ะ ทดสอบเสียง" });

        Assert.NotEmpty(result.Audio);
        Assert.False(string.IsNullOrEmpty(result.MimeType));
    }
}

public class HealthServiceTests
{
    private readonly HealthService _service = new(
        new FakeUnitOfWork(), new FakeServiceProvider(), NullLogger<IHealthService>.Instance);

    [Fact]
    public void Get_ReportsOkAndTheSelectedProviders()
    {
        var health = _service.Get();

        Assert.Equal("ok", health.Status);
        Assert.False(string.IsNullOrEmpty(health.Providers.SlidesProvider));
        Assert.False(string.IsNullOrEmpty(health.Providers.TtsProvider));
        Assert.False(string.IsNullOrEmpty(health.Providers.VoiceQuestionProvider));
    }
}
