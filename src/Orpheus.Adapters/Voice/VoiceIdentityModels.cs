namespace Orpheus.Adapters.Voice;

public enum VoiceIdentityState
{
    Active,
    Candidate,
    Rejected
}

public sealed record VoiceIdentity(
    string Id,
    string PersonaId,
    string Provider,
    string Fingerprint,
    VoiceIdentityState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? PreviewText = null,
    string? PreviewAudioUri = null);

public sealed record VoiceIdentityResolution(
    VoiceIdentity Identity,
    bool IsStale,
    string? Warning);

public sealed record VoiceIdentityStatus(
    VoiceIdentity? Active,
    IReadOnlyList<VoiceIdentity> Candidates,
    IReadOnlyList<VoiceIdentity> Rejected,
    bool ActiveIsStale,
    string? Warning);
