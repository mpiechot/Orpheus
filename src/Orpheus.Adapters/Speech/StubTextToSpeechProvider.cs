using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Speech;

public sealed class StubTextToSpeechProvider : ITextToSpeechProvider
{
    public Task<AudioResult> SynthesizeAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        var voiceId = request.Persona.Voice.VoiceId;
        var provider = request.Persona.Voice.Provider;
        var identitySegment = string.IsNullOrWhiteSpace(request.VoiceIdentityKey)
            ? string.Empty
            : $"/{Uri.EscapeDataString(request.VoiceIdentityKey)}";
        var audio = new AudioResult(
            $"stub://{Uri.EscapeDataString(voiceId)}/speech{identitySegment}",
            provider,
            voiceId,
            "audio/stub");

        return Task.FromResult(audio);
    }
}
