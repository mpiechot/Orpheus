using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Core.Models;
using Orpheus.ProcessHelper;

namespace Orpheus.Core.Tests;

public sealed class ProcessTextToSpeechProviderTests
{
    [Fact]
    public async Task Process_provider_invokes_configured_command_and_validates_output()
    {
        using var workspace = TestWorkspace.Create();
        var referenceAudio = workspace.WriteFile("reference.wav", "reference");
        var provider = CreateProvider(
            workspace,
            new StaticRuntimeMetadataResolver(CreateMetadata(workspace, referenceAudio)),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}",
                "--reference-audio",
                "{referenceAudio}"
            ],
            requireReferenceAudio: true);
        var persona = CreatePersona();

        var audio = await provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight."));

        var audioPath = new Uri(audio.Uri).LocalPath;
        Assert.Equal("process-test", audio.Provider);
        Assert.Equal("audio/wav", audio.ContentType);
        Assert.True(File.Exists(audioPath));
        Assert.True(new FileInfo(audioPath).Length > 44);
    }

    [Fact]
    public async Task Process_provider_fails_when_required_reference_audio_is_missing()
    {
        using var workspace = TestWorkspace.Create();
        var provider = CreateProvider(
            workspace,
            new EmptyRuntimeMetadataResolver(),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}",
                "--reference-audio",
                "{referenceAudio}"
            ],
            requireReferenceAudio: true);
        var persona = CreatePersona();

        var exception = await Assert.ThrowsAsync<PersonaConfigurationException>(
            () => provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight.")));

        Assert.Equal(
            "Provider 'process-test' requires voice.assets.referenceAudio for persona 'guide'.",
            exception.Message);
    }

    [Fact]
    public async Task Process_provider_reports_non_zero_exit_code()
    {
        using var workspace = TestWorkspace.Create();
        var provider = CreateProvider(
            workspace,
            new EmptyRuntimeMetadataResolver(),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}",
                "--exit-code",
                "7"
            ]);
        var persona = CreatePersona();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight.")));

        Assert.Contains("Provider 'process-test' failed with exit code 7", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Process_provider_times_out()
    {
        using var workspace = TestWorkspace.Create();
        var provider = CreateProvider(
            workspace,
            new EmptyRuntimeMetadataResolver(),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}",
                "--delay-ms",
                "1000"
            ],
            timeout: TimeSpan.FromMilliseconds(100));
        var persona = CreatePersona();

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight.")));

        Assert.Equal("Provider 'process-test' exceeded the configured timeout of 0 seconds.", exception.Message);
    }

    [Fact]
    public async Task Process_provider_reports_missing_output_file()
    {
        using var workspace = TestWorkspace.Create();
        var provider = CreateProvider(
            workspace,
            new EmptyRuntimeMetadataResolver(),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}",
                "--skip-output"
            ]);
        var persona = CreatePersona();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight.")));

        Assert.Contains("Provider 'process-test' did not create output file", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Process_provider_reports_invalid_output_file()
    {
        using var workspace = TestWorkspace.Create();
        var provider = CreateProvider(
            workspace,
            new EmptyRuntimeMetadataResolver(),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}",
                "--write-empty"
            ]);
        var persona = CreatePersona();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight.")));

        Assert.Contains("did not create a valid WAV file", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Process_provider_rejects_unsupported_local_assets()
    {
        using var workspace = TestWorkspace.Create();
        var modelPath = workspace.CreateDirectory("model");
        var metadata = new PersonaRuntimeMetadata(
            "guide",
            new PersonaSourceInfo(PersonaSourceKind.Local, Path.Combine(workspace.Root, "persona.json")),
            new PersonaVoiceAssets(null, null, modelPath, null, new Dictionary<string, string>()));
        var provider = CreateProvider(
            workspace,
            new StaticRuntimeMetadataResolver(metadata),
            [
                ProcessHelperPath,
                "--text-file",
                "{textFile}",
                "--output",
                "{outputFile}"
            ]);
        var persona = CreatePersona();

        var exception = await Assert.ThrowsAsync<PersonaConfigurationException>(
            () => provider.SynthesizeAsync(new SpeechSynthesisRequest(persona, "Continue straight.")));

        Assert.Equal(
            "Provider 'process-test' does not support voice.assets.modelPath for persona 'guide'.",
            exception.Message);
    }

    private static ProcessTextToSpeechProvider CreateProvider(
        TestWorkspace workspace,
        IPersonaRuntimeMetadataResolver resolver,
        IReadOnlyList<string> arguments,
        bool requireReferenceAudio = false,
        TimeSpan? timeout = null)
    {
        return new ProcessTextToSpeechProvider(
            new ProcessTextToSpeechProviderOptions(
                "process-test",
                "dotnet",
                arguments,
                workspace.Output,
                Timeout: timeout,
                RequireReferenceAudio: requireReferenceAudio),
            resolver);
    }

    private static string ProcessHelperPath => typeof(ProcessHelperMarker).Assembly.Location;

    private static PersonaRuntimeMetadata CreateMetadata(TestWorkspace workspace, string referenceAudio)
    {
        return new PersonaRuntimeMetadata(
            "guide",
            new PersonaSourceInfo(PersonaSourceKind.Local, Path.Combine(workspace.Root, "persona.json")),
            new PersonaVoiceAssets(null, referenceAudio, null, null, new Dictionary<string, string>()));
    }

    private static Persona CreatePersona()
    {
        return new Persona(
            "guide",
            "Guide",
            "A guide.",
            new PersonaSpeechProfile(["Use plain words."]),
            new PersonaVoiceProfile("stub", "guide-voice", ["Calm."]));
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
            return string.Equals(personaId, _metadata.PersonaId, StringComparison.OrdinalIgnoreCase)
                ? _metadata
                : null;
        }
    }

    private sealed class EmptyRuntimeMetadataResolver : IPersonaRuntimeMetadataResolver
    {
        public PersonaRuntimeMetadata? GetRuntimeMetadata(string personaId)
        {
            return null;
        }
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string root)
        {
            Root = root;
            Output = CreateDirectory("audio");
        }

        public string Root { get; }

        public string Output { get; }

        public static TestWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "orpheus-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestWorkspace(root);
        }

        public string CreateDirectory(string name)
        {
            var path = Path.Combine(Root, name);
            Directory.CreateDirectory(path);
            return path;
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
