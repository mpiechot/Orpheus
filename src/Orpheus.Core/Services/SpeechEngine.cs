using Orpheus.Core.Abstractions;
using Orpheus.Core.Exceptions;
using Orpheus.Core.Models;

namespace Orpheus.Core.Services;

public sealed class SpeechEngine
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IPersonaTransformer _personaTransformer;
    private readonly ITextToSpeechProvider _textToSpeechProvider;

    public SpeechEngine(
        IPersonaRepository personaRepository,
        IPersonaTransformer personaTransformer,
        ITextToSpeechProvider textToSpeechProvider)
    {
        _personaRepository = personaRepository;
        _personaTransformer = personaTransformer;
        _textToSpeechProvider = textToSpeechProvider;
    }

    public async Task<SpeechResult> SpeakAsync(
        SpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        var personaId = NormalizePersonaId(request.PersonaId);
        var text = NormalizeText(request.Text);

        var persona = await _personaRepository.GetByIdAsync(personaId, cancellationToken);
        if (persona is null)
        {
            throw new PersonaNotFoundException(personaId);
        }

        var transformed = await _personaTransformer.TransformAsync(persona, text, cancellationToken);
        var audio = await _textToSpeechProvider.SynthesizeAsync(
            new SpeechSynthesisRequest(persona, transformed.Text),
            cancellationToken);

        return new SpeechResult(persona.Id, transformed.Text, audio);
    }

    private static string NormalizePersonaId(string personaId)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            throw new SpeechRequestValidationException("Persona is required.");
        }

        return personaId.Trim();
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new SpeechRequestValidationException("Text is required.");
        }

        return text.Trim();
    }
}
