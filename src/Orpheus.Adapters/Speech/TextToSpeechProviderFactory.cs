using Orpheus.Core.Abstractions;
using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Voice;

namespace Orpheus.Adapters.Speech;

public sealed record TextToSpeechProviderFactoryOptions(
    string ProviderName,
    string OutputDirectory,
    string? WindowsSapiVoiceName = null,
    int WindowsSapiTimeoutSeconds = 30,
    ProcessTextToSpeechProviderOptions? ProcessOptions = null,
    IVoiceIdentityStore? VoiceIdentityStore = null,
    IPersonaRuntimeMetadataResolver? RuntimeMetadataResolver = null);

public static class TextToSpeechProviderFactory
{
    public static string DefaultProviderName => "deterministic-wav";

    public static ITextToSpeechProvider Create(
        string providerName,
        string outputDirectory,
        string? windowsSapiVoiceName = null,
        int windowsSapiTimeoutSeconds = 30)
    {
        return Create(new TextToSpeechProviderFactoryOptions(
            providerName,
            outputDirectory,
            windowsSapiVoiceName,
            windowsSapiTimeoutSeconds));
    }

    public static ITextToSpeechProvider Create(TextToSpeechProviderFactoryOptions options)
    {
        var normalizedProviderName = options.ProviderName.Trim().ToLowerInvariant();
        ITextToSpeechProvider provider = normalizedProviderName switch
        {
            "windows-sapi" => new WindowsSapiTextToSpeechProvider(
                new WindowsSapiTextToSpeechProviderOptions(
                    options.OutputDirectory,
                    options.WindowsSapiVoiceName,
                    TimeSpan.FromSeconds(options.WindowsSapiTimeoutSeconds))),
            "deterministic-wav" => new DeterministicWavTextToSpeechProvider(
                new DeterministicWavTextToSpeechProviderOptions(options.OutputDirectory)),
            "process" => CreateProcessProvider(options),
            "stub" => new StubTextToSpeechProvider(),
            _ => throw new InvalidOperationException($"Unsupported text-to-speech provider '{options.ProviderName}'.")
        };

        var identityProviderName = normalizedProviderName == "process"
            ? options.ProcessOptions?.ProviderName ?? normalizedProviderName
            : normalizedProviderName;

        return options.VoiceIdentityStore is null
            ? provider
            : new VoiceIdentityTextToSpeechProvider(provider, options.VoiceIdentityStore, identityProviderName);
    }

    private static ITextToSpeechProvider CreateProcessProvider(TextToSpeechProviderFactoryOptions options)
    {
        if (options.ProcessOptions is null)
        {
            throw new InvalidOperationException("Process text-to-speech provider requires process options.");
        }

        if (options.RuntimeMetadataResolver is null)
        {
            throw new InvalidOperationException("Process text-to-speech provider requires runtime metadata resolution.");
        }

        return new ProcessTextToSpeechProvider(options.ProcessOptions, options.RuntimeMetadataResolver);
    }
}
