namespace SupportRoom.Providers.Tts;

public sealed class TtsInput
{
    public required string Text { get; init; }
    public string? Voice { get; init; }

    /// <summary>
    /// SSML rate override (e.g. "-45%") for utterances that shouldn't run at lesson pace. Falls
    /// back to EDGE_TTS_RATE when omitted.
    /// </summary>
    public string? Rate { get; init; }
}

public sealed class TtsResult
{
    public required byte[] Audio { get; init; }
    public required string MimeType { get; init; }
}

public interface ITtsProvider
{
    Task<TtsResult> SynthesizeAsync(TtsInput input);
}
