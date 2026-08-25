using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.VoiceQuestion;

namespace SupportRoom.Providers.Tests;

/// <summary>
/// Unit-level (no real Gemini/Pinecone call) coverage for TQ-10: AnswerTextAsync must reject a
/// provider response whose answerStatus is no_speech/transcription_failed rather than persisting
/// it as if it were a usable answer. Regression test for the bug where AnswerTextAsync checked
/// GeminiRest.IsAnswerStatus() - the UNION used by both the voice and text paths, which includes
/// those two voice-only statuses - instead of the narrower set the text path actually allows.
///
/// A fake IHttpClientFactory stands in for the real Gemini REST call so this test is deterministic
/// and does not depend on a live GEMINI_API_KEY or network access; a fake IEmbeddingProvider/
/// IKnowledgeIndexProvider that finds nothing indexed forces the full-deck fallback path (KS-11),
/// which needs no Pinecone either.
/// </summary>
public class RagVoiceQuestionProviderAnswerTextTests
{
    private static readonly VoiceQuestionSlideContext[] Slides =
        [new() { SlideObjectId = "slide-1", SpeakerNotes = "หน้าแรกของบทเรียน" }];

    private static TextQuestionInput Input() => new()
    {
        QuestionText = "นี่คือหน้าอะไร",
        LessonSlides = Slides,
        LessonNamespace = "lesson-ns",
        CategoryNamespace = "category-ns",
        GlobalNamespace = "global-ns",
    };

    private static RagVoiceQuestionProvider CreateProvider(string geminiAnswerJson)
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "fake-key-for-test");
        var httpClientFactory = new FakeGeminiHttpClientFactory(geminiAnswerJson);
        return new RagVoiceQuestionProvider(
            httpClientFactory,
            new FakeEmbeddingProvider(),
            new FakeEmptyKnowledgeIndexProvider(),
            NullLogger<RagVoiceQuestionProvider>.Instance);
    }

    [Fact]
    public async Task AnswerTextAsync_ThrowsWhenProviderReturnsNoSpeech()
    {
        var provider = CreateProvider("""{"answer":null,"answerStatus":"no_speech","relatedSlideObjectId":null,"conflict":null}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AnswerTextAsync(Input()));
    }

    [Fact]
    public async Task AnswerTextAsync_ThrowsWhenProviderReturnsTranscriptionFailed()
    {
        var provider = CreateProvider("""{"answer":null,"answerStatus":"transcription_failed","relatedSlideObjectId":null,"conflict":null}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AnswerTextAsync(Input()));
    }

    [Theory]
    [InlineData(AnswerStatus.Answered)]
    [InlineData(AnswerStatus.NotFound)]
    [InlineData(AnswerStatus.OutOfScope)]
    public async Task AnswerTextAsync_AcceptsEveryStatusTheTextPathAllows(string status)
    {
        var provider = CreateProvider($$"""{"answer":"คำตอบ","answerStatus":"{{status}}","relatedSlideObjectId":null,"conflict":null}""");

        var result = await provider.AnswerTextAsync(Input());

        Assert.Equal(status, result.AnswerStatus);
    }

    private sealed class FakeEmbeddingProvider : IEmbeddingProvider
    {
        public Task<float[]> EmbedAsync(string text, EmbeddingTaskType taskType) => Task.FromResult(new float[] { 0f });
    }

    /// <summary>Empty result from every namespace forces the full-deck fallback (KS-11) - no
    /// Pinecone call needed for this test's purpose, which is only about the answer-status gate.</summary>
    private sealed class FakeEmptyKnowledgeIndexProvider : IKnowledgeIndexProvider
    {
        public Task UpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeChunk> chunks) => Task.CompletedTask;
        public Task<IReadOnlyList<ScoredChunk>> QueryAsync(string namespaceKey, float[] queryVector, int topK) => Task.FromResult<IReadOnlyList<ScoredChunk>>([]);
        public Task DeleteNamespaceAsync(string namespaceKey) => Task.CompletedTask;
        public Task DeleteVectorsAsync(string namespaceKey, IReadOnlyList<string> ids) => Task.CompletedTask;
        public Task UpdateMetadataAsync(string namespaceKey, string id, string text, IReadOnlyDictionary<string, string>? metadata) => Task.CompletedTask;
    }

    private sealed class FakeGeminiHttpClientFactory(string geminiAnswerJson) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeGeminiHandler(geminiAnswerJson));
    }

    private sealed class FakeGeminiHandler(string geminiAnswerJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    candidates = new[]
                    {
                        new { content = new { parts = new[] { new { text = geminiAnswerJson } } } },
                    },
                }),
            };
            return Task.FromResult(response);
        }
    }
}
