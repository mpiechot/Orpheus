namespace Orpheus.Core.Models;

public sealed record Persona(
    string Id,
    string DisplayName,
    string Description,
    PersonaSpeechProfile Speech,
    PersonaVoiceProfile Voice,
    string? PreviewText = null);

public sealed record PersonaSpeechProfile(IReadOnlyList<string> Style);

public sealed record PersonaVoiceProfile(
    string Provider,
    string VoiceId,
    IReadOnlyList<string> Style);
