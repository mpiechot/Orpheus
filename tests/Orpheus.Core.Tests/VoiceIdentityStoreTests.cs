using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Voice;
using Orpheus.Core.Models;

namespace Orpheus.Core.Tests;

public sealed class VoiceIdentityStoreTests
{
    [Fact]
    public async Task Store_creates_and_reuses_active_voice_identity()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(workspace.Root));
        var persona = CreatePersona("calm-guide", voiceId: "voice-a");

        var first = await store.GetOrCreateActiveAsync(persona, "deterministic-wav");
        var second = await store.GetOrCreateActiveAsync(persona, "deterministic-wav");

        Assert.Equal(VoiceIdentityState.Active, first.Identity.State);
        Assert.Equal(first.Identity.Id, second.Identity.Id);
        Assert.False(second.IsStale);
        Assert.Null(second.Warning);
        Assert.Single(Directory.EnumerateFiles(workspace.Root, "*.json"));
    }

    [Fact]
    public async Task Store_reports_stale_active_voice_when_persona_fingerprint_changes()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(workspace.Root));
        var original = CreatePersona("calm-guide", voiceId: "voice-a");
        var changed = CreatePersona("calm-guide", voiceId: "voice-b");

        var active = await store.GetOrCreateActiveAsync(original, "deterministic-wav");
        var resolved = await store.GetOrCreateActiveAsync(changed, "deterministic-wav");
        var status = await store.GetStatusAsync(changed, "deterministic-wav");

        Assert.Equal(active.Identity.Id, resolved.Identity.Id);
        Assert.True(resolved.IsStale);
        Assert.True(status.ActiveIsStale);
        Assert.Equal("Active voice for persona 'calm-guide' and provider 'deterministic-wav' is stale.", resolved.Warning);
    }

    [Fact]
    public async Task Store_creates_candidate_and_accepts_it_as_active()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(workspace.Root));
        var persona = CreatePersona("calm-guide", voiceId: "voice-a");
        var active = await store.GetOrCreateActiveAsync(persona, "deterministic-wav");

        var candidate = await store.CreateCandidateAsync(persona, "deterministic-wav", "Preview this voice.");
        var accepted = await store.AcceptAsync(persona.Id, "deterministic-wav", candidate.Id);
        var status = await store.GetStatusAsync(persona, "deterministic-wav");

        Assert.Equal(candidate.Id, accepted.Id);
        Assert.Equal(VoiceIdentityState.Active, accepted.State);
        Assert.Equal(candidate.Id, status.Active?.Id);
        Assert.DoesNotContain(status.Candidates, identity => identity.Id == candidate.Id);
        Assert.Contains(status.Rejected, identity => identity.Id == active.Identity.Id);
    }

    [Fact]
    public async Task Store_rejects_candidate_without_changing_active_voice()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(workspace.Root));
        var persona = CreatePersona("calm-guide", voiceId: "voice-a");
        var active = await store.GetOrCreateActiveAsync(persona, "deterministic-wav");
        var candidate = await store.CreateCandidateAsync(persona, "deterministic-wav", "Preview this voice.");

        var rejected = await store.RejectAsync(persona.Id, "deterministic-wav", candidate.Id);
        var status = await store.GetStatusAsync(persona, "deterministic-wav");

        Assert.Equal(VoiceIdentityState.Rejected, rejected.State);
        Assert.Equal(active.Identity.Id, status.Active?.Id);
        Assert.Empty(status.Candidates);
        Assert.Contains(status.Rejected, identity => identity.Id == candidate.Id);
    }

    [Fact]
    public async Task Store_includes_local_asset_metadata_in_fingerprint()
    {
        using var workspace = TestWorkspace.Create();
        var referenceAudio = workspace.WriteFile("reference.wav", "first");
        var resolver = new StaticRuntimeMetadataResolver(new PersonaRuntimeMetadata(
            "calm-guide",
            new PersonaSourceInfo(PersonaSourceKind.Local, Path.Combine(workspace.Root, "persona.json")),
            new PersonaVoiceAssets(null, referenceAudio, null, null, new Dictionary<string, string>())));
        var store = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(workspace.Root, resolver));
        var persona = CreatePersona("calm-guide", voiceId: "voice-a");
        var active = await store.GetOrCreateActiveAsync(persona, "deterministic-wav");

        await Task.Delay(20);
        File.WriteAllText(referenceAudio, "changed");
        var resolved = await store.GetOrCreateActiveAsync(persona, "deterministic-wav");

        Assert.Equal(active.Identity.Id, resolved.Identity.Id);
        Assert.True(resolved.IsStale);
    }

    private static Persona CreatePersona(string id, string voiceId)
    {
        return new Persona(
            id,
            "Calm Guide",
            "A calm guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", voiceId, ["Calm."]));
    }

    private sealed class StaticRuntimeMetadataResolver : IPersonaRuntimeMetadataResolver
    {
        private readonly PersonaRuntimeMetadata _metadata;

        public StaticRuntimeMetadataResolver(PersonaRuntimeMetadata metadata)
        {
            _metadata = metadata;
        }

        public PersonaRuntimeMetadata? GetRuntimeMetadata(string personaId)
        {
            return string.Equals(_metadata.PersonaId, personaId, StringComparison.OrdinalIgnoreCase)
                ? _metadata
                : null;
        }
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

        public string WriteFile(string fileName, string contents)
        {
            var path = Path.Combine(Root, fileName);
            File.WriteAllText(path, contents);
            return path;
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
