using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.Transformation;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Exceptions;
using Orpheus.Core.Models;
using Orpheus.Core.Services;

namespace Orpheus.Core.Tests;

public class SpeechEngineTests
{
    [Fact]
    public void Persona_can_be_created_with_speech_and_voice_profiles()
    {
        var persona = new Persona(
            "calm-guide",
            "Calm Guide",
            "A calm neutral guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "calm-guide-placeholder", ["Calm."]));

        Assert.Equal("calm-guide", persona.Id);
        Assert.Equal("Use plain words.", persona.Speech.Style.Single());
        Assert.Equal("calm-guide-placeholder", persona.Voice.VoiceId);
        Assert.Null(persona.PreviewText);
    }

    [Fact]
    public void Persona_can_include_optional_preview_text()
    {
        var persona = new Persona(
            "calm-guide",
            "Calm Guide",
            "A calm neutral guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "calm-guide-placeholder", ["Calm."]),
            "We can continue carefully when the path changes.");

        Assert.Equal("We can continue carefully when the path changes.", persona.PreviewText);
    }

    [Fact]
    public async Task SpeakAsync_transforms_text_and_creates_audio_result()
    {
        var speechEngine = CreateSpeechEngine();

        var result = await speechEngine.SpeakAsync(
            new SpeechRequest("wise-master", "In 500 meters, turn right."));

        Assert.Equal("wise-master", result.PersonaId);
        Assert.Equal("In 500 meters, turn right, you should.", result.Text);
        Assert.Equal("stub://wise-master-placeholder/speech", result.Audio.Uri);
        Assert.Equal("stub", result.Audio.Provider);
        Assert.Equal("wise-master-placeholder", result.Audio.VoiceId);
        Assert.Equal("audio/stub", result.Audio.ContentType);
    }

    [Fact]
    public async Task SpeakAsync_stores_last_original_text_without_transformed_output()
    {
        var lastSpeechTextStore = new RecordingLastSpeechTextStore();
        var speechEngine = CreateSpeechEngine(lastSpeechTextStore);

        await speechEngine.SpeakAsync(
            new SpeechRequest("wise-master", "  In 500 meters, turn right.  "));

        Assert.Equal("wise-master", lastSpeechTextStore.PersonaId);
        Assert.Equal("In 500 meters, turn right.", lastSpeechTextStore.OriginalText);
    }

    [Fact]
    public async Task SpeakAsync_rejects_unknown_persona()
    {
        var speechEngine = CreateSpeechEngine();

        var exception = await Assert.ThrowsAsync<PersonaNotFoundException>(
            () => speechEngine.SpeakAsync(new SpeechRequest("missing-persona", "Hello.")));

        Assert.Equal("missing-persona", exception.PersonaId);
    }

    [Fact]
    public async Task SpeakAsync_rejects_empty_text()
    {
        var speechEngine = CreateSpeechEngine();

        var exception = await Assert.ThrowsAsync<SpeechRequestValidationException>(
            () => speechEngine.SpeakAsync(new SpeechRequest("wise-master", "   ")));

        Assert.Equal("Text is required.", exception.Message);
    }

    private static SpeechEngine CreateSpeechEngine(ILastSpeechTextStore? lastSpeechTextStore = null)
    {
        return new SpeechEngine(
            InMemoryPersonaRepository.CreateWithSamplePersonas(),
            new StubPersonaTransformer(),
            new StubTextToSpeechProvider(),
            lastSpeechTextStore);
    }

    private sealed class RecordingLastSpeechTextStore : ILastSpeechTextStore
    {
        public string? PersonaId { get; private set; }

        public string? OriginalText { get; private set; }

        public Task StoreAsync(string personaId, string originalText, CancellationToken cancellationToken = default)
        {
            PersonaId = personaId;
            OriginalText = originalText;
            return Task.CompletedTask;
        }

        public Task<string?> GetLastOriginalTextAsync(string personaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OriginalText);
        }
    }
}
