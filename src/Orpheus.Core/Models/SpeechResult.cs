namespace Orpheus.Core.Models;

public sealed record SpeechResult(string PersonaId, string Text, AudioResult Audio);

public sealed record PersonaTextResult(string PersonaId, string Text);

public sealed record SpeechSynthesisRequest(Persona Persona, string Text);

public sealed record AudioResult(
    string Uri,
    string Provider,
    string VoiceId,
    string ContentType);
