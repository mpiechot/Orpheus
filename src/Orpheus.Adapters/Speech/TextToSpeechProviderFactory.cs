using Orpheus.Core.Abstractions;

namespace Orpheus.Adapters.Speech;

public static class TextToSpeechProviderFactory
{
    public static string DefaultProviderName => "deterministic-wav";

    public static ITextToSpeechProvider Create(
        string providerName,
        string outputDirectory,
        string? windowsSapiVoiceName = null,
        int windowsSapiTimeoutSeconds = 30)
    {
        return providerName.Trim().ToLowerInvariant() switch
        {
            "windows-sapi" => new WindowsSapiTextToSpeechProvider(
                new WindowsSapiTextToSpeechProviderOptions(
                    outputDirectory,
                    windowsSapiVoiceName,
                    TimeSpan.FromSeconds(windowsSapiTimeoutSeconds))),
            "deterministic-wav" => new DeterministicWavTextToSpeechProvider(
                new DeterministicWavTextToSpeechProviderOptions(outputDirectory)),
            "stub" => new StubTextToSpeechProvider(),
            _ => throw new InvalidOperationException($"Unsupported text-to-speech provider '{providerName}'.")
        };
    }
}
