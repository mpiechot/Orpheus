using Orpheus.Adapters.Personas;

namespace Orpheus.Core.Tests;

public sealed class FilePersonaRepositoryTests
{
    [Fact]
    public async Task Repository_loads_committed_personas_from_json()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        workspace.WritePersona(committed, "calm-guide.json", PersonaJson("calm-guide", "Calm Guide", "committed-voice"));

        var repository = FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed));

        var persona = await repository.GetByIdAsync("calm-guide");

        Assert.NotNull(persona);
        Assert.Equal("Calm Guide", persona.DisplayName);
        Assert.Equal("committed-voice", persona.Voice.VoiceId);
        Assert.Equal("Preview text for calm-guide.", persona.PreviewText);
        Assert.Equal(PersonaSourceKind.Committed, repository.GetRuntimeMetadata("calm-guide")?.Source.Kind);
    }

    [Fact]
    public async Task Repository_ignores_missing_local_persona_directory()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        var missingLocal = Path.Combine(workspace.Root, ".orpheus", "personas");
        workspace.WritePersona(committed, "calm-guide.json", PersonaJson("calm-guide", "Calm Guide", "committed-voice"));

        var repository = FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed, missingLocal));

        var personas = await repository.ListAsync();

        Assert.Single(personas);
        Assert.Equal("committed-voice", personas.Single().Voice.VoiceId);
    }

    [Fact]
    public async Task Repository_loads_local_only_personas()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        var local = workspace.CreateDirectory(".orpheus", "personas");
        workspace.WritePersona(local, "local-guide.json", PersonaJson("local-guide", "Local Guide", "local-voice"));

        var repository = FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed, local));

        var persona = await repository.GetByIdAsync("local-guide");

        Assert.NotNull(persona);
        Assert.Equal("Local Guide", persona.DisplayName);
        Assert.Equal(PersonaSourceKind.Local, repository.GetRuntimeMetadata("local-guide")?.Source.Kind);
    }

    [Fact]
    public async Task Local_persona_overrides_committed_persona_with_same_id()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        var local = workspace.CreateDirectory(".orpheus", "personas");
        workspace.WritePersona(committed, "guide.json", PersonaJson("guide", "Committed Guide", "committed-voice"));
        workspace.WritePersona(local, "guide.json", PersonaJson("guide", "Local Guide", "local-voice"));

        var repository = FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed, local));

        var persona = await repository.GetByIdAsync("guide");

        Assert.NotNull(persona);
        Assert.Equal("Local Guide", persona.DisplayName);
        Assert.Equal("local-voice", persona.Voice.VoiceId);
        Assert.Equal(PersonaSourceKind.Local, repository.GetRuntimeMetadata("guide")?.Source.Kind);
    }

    [Fact]
    public void Committed_personas_reject_voice_assets()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        workspace.WritePersona(
            committed,
            "guide.json",
            PersonaJson("guide", "Guide", "committed-voice", assetsJson: """
                "assets": {
                  "speakerSample": "sample.wav"
                }
                """));

        var exception = Assert.Throws<PersonaConfigurationException>(
            () => FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed)));

        Assert.Contains("must not contain 'voice.assets'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_personas_accept_runtime_voice_assets_and_resolve_relative_paths()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        var local = workspace.CreateDirectory(".orpheus", "personas");
        workspace.WritePersona(
            local,
            "guide.json",
            PersonaJson("guide", "Guide", "local-voice", assetsJson: """
                "assets": {
                  "referenceAudio": "voices\\reference.wav",
                  "modelPath": "models\\voice-model",
                  "providerSettings": {
                    "language": "de",
                    "stability": "balanced"
                  }
                }
                """));

        var repository = FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed, local));

        var metadata = repository.GetRuntimeMetadata("guide");

        Assert.NotNull(metadata);
        Assert.Equal(Path.GetFullPath(Path.Combine(local, "voices", "reference.wav")), metadata.VoiceAssets?.ReferenceAudio);
        Assert.Equal(Path.GetFullPath(Path.Combine(local, "models", "voice-model")), metadata.VoiceAssets?.ModelPath);
        Assert.Equal("\"de\"", metadata.VoiceAssets?.ProviderSettings["language"]);
        Assert.Equal("\"balanced\"", metadata.VoiceAssets?.ProviderSettings["stability"]);
    }

    [Fact]
    public void Local_provider_specific_voice_values_must_be_under_provider_settings()
    {
        using var workspace = TestWorkspace.Create();
        var committed = workspace.CreateDirectory("samples");
        var local = workspace.CreateDirectory(".orpheus", "personas");
        workspace.WritePersona(
            local,
            "guide.json",
            """
            {
              "id": "guide",
              "displayName": "Guide",
              "description": "A guide.",
              "speech": {
                "style": [ "Use plain words." ]
              },
              "voice": {
                "provider": "local",
                "voiceId": "local-voice",
                "temperature": 0.5,
                "style": [ "Calm." ]
              }
            }
            """);

        var exception = Assert.Throws<PersonaConfigurationException>(
            () => FilePersonaRepository.Load(new FilePersonaRepositoryOptions(committed, local)));

        Assert.Contains("provider-specific voice field 'temperature'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Required_runtime_metadata_reports_clear_configuration_error_when_missing()
    {
        IPersonaRuntimeMetadataResolver resolver = new EmptyRuntimeMetadataResolver();

        var exception = Assert.Throws<PersonaConfigurationException>(
            () => resolver.RequireRuntimeMetadata("missing-persona", "local-provider"));

        Assert.Equal(
            "Provider 'local-provider' requires runtime metadata for persona 'missing-persona', but none was resolved.",
            exception.Message);
    }

    private static string PersonaJson(
        string id,
        string displayName,
        string voiceId,
        string? assetsJson = null)
    {
        var assets = string.IsNullOrWhiteSpace(assetsJson)
            ? string.Empty
            : $"{assetsJson},";

        return $$"""
            {
              "id": "{{id}}",
              "displayName": "{{displayName}}",
              "description": "A test persona.",
              "previewText": "Preview text for {{id}}.",
              "speech": {
                "style": [ "Use plain words." ]
              },
              "voice": {
                "provider": "stub",
                "voiceId": "{{voiceId}}",
                {{assets}}
                "style": [ "Calm." ]
              }
            }
            """;
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

        public string CreateDirectory(params string[] segments)
        {
            var path = Path.Combine(new[] { Root }.Concat(segments).ToArray());
            Directory.CreateDirectory(path);
            return path;
        }

        public void WritePersona(string directory, string fileName, string json)
        {
            File.WriteAllText(Path.Combine(directory, fileName), json);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class EmptyRuntimeMetadataResolver : IPersonaRuntimeMetadataResolver
    {
        public PersonaRuntimeMetadata? GetRuntimeMetadata(string personaId)
        {
            return null;
        }
    }
}
