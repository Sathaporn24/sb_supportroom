namespace SupportRoom.Application.Tests;

/// <summary>
/// Tests construct Real providers directly (not through DI), so Gemini/Pinecone-backed ones need
/// a genuine IHttpClientFactory to call. This is not a mock of network behavior - CreateClient
/// still hands back a real HttpClient that makes real requests - just a minimal stand-in for
/// ASP.NET Core's named-client registration/pooling, which a plain constructor call bypasses.
/// </summary>
internal sealed class RealHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}
