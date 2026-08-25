using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Providers.VoiceQuestion;

namespace SupportRoom.Providers.Tests;

public class GeminiTimeoutTests
{
    [Fact]
    public async Task AnswerTextAsync_AppliesBoundedTimeoutToGeminiClient()
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "fake-key-for-test");
        var factory = new CapturingHttpClientFactory();
        var provider = new GeminiVoiceQuestionProvider(factory, NullLogger<GeminiVoiceQuestionProvider>.Instance);

        await provider.AnswerTextAsync(new TextQuestionInput
        {
            QuestionText = "คำถาม",
            LessonSlides = [new VoiceQuestionSlideContext { SlideObjectId = "slide-1", SpeakerNotes = "เนื้อหา" }],
            LessonNamespace = "lesson-ns",
            CategoryNamespace = "category-ns",
            GlobalNamespace = "global-ns",
        });

        Assert.Equal(TimeSpan.FromSeconds(20), factory.Client?.Timeout);
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient? Client { get; private set; }

        public HttpClient CreateClient(string name)
        {
            Client = new HttpClient(new SuccessfulGeminiHandler());
            return Client;
        }
    }

    private sealed class SuccessfulGeminiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string answerJson = """{"answer":"คำตอบ","answerStatus":"answered","relatedSlideObjectId":null}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    candidates = new[]
                    {
                        new { content = new { parts = new[] { new { text = answerJson } } } },
                    },
                }),
            });
        }
    }
}
