using Orpheus.Adapters.Speech;
using Orpheus.Adapters.Voice;
using Orpheus.Core.Models;

namespace Orpheus.Core.Tests;

public sealed class TextToSpeechProviderTests
{
    [Fact]
    public async Task Deterministic_wav_provider_writes_valid_audio_file()
    {
        using var workspace = TestWorkspace.Create();
        var provider = new DeterministicWavTextToSpeechProvider(
            new DeterministicWavTextToSpeechProviderOptions(workspace.Root));
        var persona = new Persona(
            "calm-guide",
            "Calm Guide",
            "A calm neutral guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "calm-guide-placeholder", ["Calm."]));

        var audio = await provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight."));

        var audioPath = new Uri(audio.Uri).LocalPath;
        Assert.Equal("deterministic-wav", audio.Provider);
        Assert.Equal("audio/wav", audio.ContentType);
        Assert.True(File.Exists(audioPath));
        Assert.Equal("RIFF"u8.ToArray(), File.ReadAllBytes(audioPath)[..4]);
        Assert.True(new FileInfo(audioPath).Length > 44);
    }

    [Fact]
    public async Task Deterministic_wav_provider_reuses_same_file_for_equivalent_request()
    {
        using var workspace = TestWorkspace.Create();
        var provider = new DeterministicWavTextToSpeechProvider(
            new DeterministicWavTextToSpeechProviderOptions(workspace.Root));
        var persona = new Persona(
            "calm-guide",
            "Calm Guide",
            "A calm neutral guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "calm-guide-placeholder", ["Calm."]));
        var request = new SpeechSynthesisRequest(persona, "Continue straight.");

        var first = await provider.SynthesizeAsync(request);
        var second = await provider.SynthesizeAsync(request);

        Assert.Equal(first.Uri, second.Uri);
        Assert.Single(Directory.EnumerateFiles(workspace.Root, "*.wav"));
    }

    [Fact]
    public async Task Deterministic_wav_provider_uses_voice_identity_key_in_cache_path()
    {
        using var workspace = TestWorkspace.Create();
        var provider = new DeterministicWavTextToSpeechProvider(
            new DeterministicWavTextToSpeechProviderOptions(workspace.Root));
        var persona = new Persona(
            "calm-guide",
            "Calm Guide",
            "A calm neutral guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "calm-guide-placeholder", ["Calm."]));

        var first = await provider.SynthesizeAsync(new SpeechSynthesisRequest(
            persona,
            "Continue straight.",
            "voice-fingerprint-a"));
        var second = await provider.SynthesizeAsync(new SpeechSynthesisRequest(
            persona,
            "Continue straight.",
            "voice-fingerprint-b"));

        Assert.NotEqual(first.Uri, second.Uri);
        Assert.Equal(2, Directory.EnumerateFiles(workspace.Root, "*.wav").Count());
    }

    [Fact]
    public async Task Voice_identity_provider_creates_active_identity_before_synthesis()
    {
        using var workspace = TestWorkspace.Create();
        var voiceStore = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(workspace.Root));
        var provider = new VoiceIdentityTextToSpeechProvider(
            new StubTextToSpeechProvider(),
            voiceStore,
            "stub");
        var persona = new Persona(
            "calm-guide",
            "Calm Guide",
            "A calm neutral guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "calm-guide-placeholder", ["Calm."]));

        var audio = await provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight."));
        var status = await voiceStore.GetStatusAsync(persona, "stub");

        Assert.NotNull(status.Active);
        Assert.Contains(status.Active.Fingerprint, audio.Uri, StringComparison.Ordinal);
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TestWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "orpheus-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestWorkspace(root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
