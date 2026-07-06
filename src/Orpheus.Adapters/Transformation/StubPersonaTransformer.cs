using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Transformation;

public sealed class StubPersonaTransformer : IPersonaTransformer
{
    public Task<PersonaTextResult> TransformAsync(
        Persona persona,
        string text,
        CancellationToken cancellationToken = default)
    {
        var transformedText = persona.Id switch
        {
            "wise-master" => $"{RemoveTerminalPunctuation(text)}, you should.",
            "pirate-narrator" => $"Ahoy, {EnsureTerminalPunctuation(text)}",
            "sarcastic-ai" => $"{EnsureTerminalPunctuation(text)} Naturally.",
            "portal-announcer" => $"Test subject notice: {EnsureTerminalPunctuation(text)}",
            _ => EnsureTerminalPunctuation(text)
        };

        return Task.FromResult(new PersonaTextResult(persona.Id, transformedText));
    }

    private static string RemoveTerminalPunctuation(string text)
    {
        return text.Trim().TrimEnd('.', '!', '?');
    }

    private static string EnsureTerminalPunctuation(string text)
    {
        var trimmed = text.Trim();
        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?')
            ? trimmed
            : $"{trimmed}.";
    }
}
