using Orpheus.Core.Models;

namespace Orpheus.Adapters.Voice;

public interface IVoiceIdentityStore
{
    Task<VoiceIdentityResolution> GetOrCreateActiveAsync(
        Persona persona,
        string providerName,
        CancellationToken cancellationToken = default);

    Task<VoiceIdentityStatus> GetStatusAsync(
        Persona persona,
        string providerName,
        CancellationToken cancellationToken = default);

    Task<VoiceIdentity> CreateCandidateAsync(
        Persona persona,
        string providerName,
        string previewText,
        CancellationToken cancellationToken = default);

    Task<VoiceIdentity> SetCandidatePreviewAudioAsync(
        string personaId,
        string providerName,
        string candidateId,
        string previewAudioUri,
        CancellationToken cancellationToken = default);

    Task<VoiceIdentity> AcceptAsync(
        string personaId,
        string providerName,
        string candidateId,
        CancellationToken cancellationToken = default);

    Task<VoiceIdentity> RejectAsync(
        string personaId,
        string providerName,
        string candidateId,
        CancellationToken cancellationToken = default);
}
