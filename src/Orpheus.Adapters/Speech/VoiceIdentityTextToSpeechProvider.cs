using Orpheus.Adapters.Voice;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Speech;

public sealed class VoiceIdentityTextToSpeechProvider : ITextToSpeechProvider
{
    private readonly ITextToSpeechProvider _innerProvider;
    private readonly IVoiceIdentityStore _voiceIdentityStore;
    private readonly string _providerName;

    public VoiceIdentityTextToSpeechProvider(
        ITextToSpeechProvider innerProvider,
        IVoiceIdentityStore voiceIdentityStore,
        string providerName)
    {
        _innerProvider = innerProvider;
        _voiceIdentityStore = voiceIdentityStore;
        _providerName = providerName;
    }

    public async Task<AudioResult> SynthesizeAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolution = await _voiceIdentityStore.GetOrCreateActiveAsync(
            request.Persona,
            _providerName,
            cancellationToken);

        return await _innerProvider.SynthesizeAsync(
            request with { VoiceIdentityKey = resolution.Identity.Fingerprint },
            cancellationToken);
    }
}
