using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Speech;

public sealed record DeterministicWavTextToSpeechProviderOptions(
    string OutputDirectory);

public sealed class DeterministicWavTextToSpeechProvider : ITextToSpeechProvider
{
    private const string ProviderName = "deterministic-wav";
    private readonly DeterministicWavTextToSpeechProviderOptions _options;

    public DeterministicWavTextToSpeechProvider(DeterministicWavTextToSpeechProviderOptions options)
    {
        _options = options;
    }

    public async Task<AudioResult> SynthesizeAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.OutputDirectory);
        var outputPath = SpeechAudioFileNamer.GetOutputPath(_options.OutputDirectory, request, ProviderName, "wav");

        if (!File.Exists(outputPath))
        {
            await WriteWaveFileAsync(outputPath, request.Text, cancellationToken);
        }

        return new AudioResult(
            new Uri(outputPath).AbsoluteUri,
            ProviderName,
            request.Persona.Voice.VoiceId,
            "audio/wav");
    }

    private static async Task WriteWaveFileAsync(
        string outputPath,
        string text,
        CancellationToken cancellationToken)
    {
        const int sampleRate = 22050;
        const short channels = 1;
        const short bitsPerSample = 16;
        const short blockAlign = channels * bitsPerSample / 8;
        const int byteRate = sampleRate * blockAlign;
        var durationSeconds = Math.Clamp(0.8 + (text.Length * 0.018), 1.0, 6.0);
        var sampleCount = (int)(sampleRate * durationSeconds);
        var dataSize = sampleCount * blockAlign;

        await using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        var seed = text.Aggregate(0, (current, character) => current + character);
        var baseFrequency = 180 + (seed % 220);

        for (var i = 0; i < sampleCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var t = i / (double)sampleRate;
            var envelope = Math.Min(1.0, Math.Min(i / 800.0, (sampleCount - i) / 800.0));
            var wordPulse = 1.0 + (0.12 * Math.Sin(2 * Math.PI * 3.5 * t));
            var sample = Math.Sin(2 * Math.PI * baseFrequency * wordPulse * t) * envelope * 0.22;
            writer.Write((short)(sample * short.MaxValue));
        }
    }
}
