using System.Text.Json;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Personas;

public sealed record FilePersonaRepositoryOptions(
    string CommittedPersonasDirectory,
    string? LocalPersonasDirectory = null);

public sealed class FilePersonaRepository : IPersonaRepository, IPersonaRuntimeMetadataResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly IReadOnlyDictionary<string, Persona> _personas;
    private readonly IReadOnlyDictionary<string, PersonaRuntimeMetadata> _metadata;

    private FilePersonaRepository(
        IReadOnlyDictionary<string, Persona> personas,
        IReadOnlyDictionary<string, PersonaRuntimeMetadata> metadata)
    {
        _personas = personas;
        _metadata = metadata;
    }

    public static FilePersonaRepository Load(FilePersonaRepositoryOptions options)
    {
        if (!Directory.Exists(options.CommittedPersonasDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Committed persona directory '{options.CommittedPersonasDirectory}' was not found.");
        }

        var personas = new Dictionary<string, Persona>(StringComparer.OrdinalIgnoreCase);
        var metadata = new Dictionary<string, PersonaRuntimeMetadata>(StringComparer.OrdinalIgnoreCase);

        LoadDirectory(options.CommittedPersonasDirectory, PersonaSourceKind.Committed, personas, metadata);

        if (!string.IsNullOrWhiteSpace(options.LocalPersonasDirectory)
            && Directory.Exists(options.LocalPersonasDirectory))
        {
            LoadDirectory(options.LocalPersonasDirectory, PersonaSourceKind.Local, personas, metadata);
        }

        return new FilePersonaRepository(personas, metadata);
    }

    public Task<Persona?> GetByIdAsync(string personaId, CancellationToken cancellationToken = default)
    {
        _personas.TryGetValue(personaId, out var persona);
        return Task.FromResult(persona);
    }

    public Task<IReadOnlyCollection<Persona>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Persona>>(_personas.Values.ToArray());
    }

    public PersonaRuntimeMetadata? GetRuntimeMetadata(string personaId)
    {
        _metadata.TryGetValue(personaId, out var metadata);
        return metadata;
    }

    private static void LoadDirectory(
        string directory,
        PersonaSourceKind sourceKind,
        IDictionary<string, Persona> personas,
        IDictionary<string, PersonaRuntimeMetadata> metadata)
    {
        foreach (var filePath in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var loaded = LoadPersonaFile(filePath, sourceKind);
            personas[loaded.Persona.Id] = loaded.Persona;
            metadata[loaded.Persona.Id] = loaded.Metadata;
        }
    }

    private static LoadedPersona LoadPersonaFile(string filePath, PersonaSourceKind sourceKind)
    {
        using var jsonDocument = JsonDocument.Parse(File.ReadAllText(filePath), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        ValidateRawDocument(jsonDocument.RootElement, sourceKind, filePath);

        var document = jsonDocument.RootElement.Deserialize<PersonaDocument>(JsonOptions)
            ?? throw new PersonaConfigurationException($"Persona file '{filePath}' could not be read.");

        var persona = ToPersona(document, filePath);
        var assets = sourceKind == PersonaSourceKind.Local
            ? ToVoiceAssets(document.Voice?.Assets, filePath)
            : null;

        var metadata = new PersonaRuntimeMetadata(
            persona.Id,
            new PersonaSourceInfo(sourceKind, Path.GetFullPath(filePath)),
            assets);

        return new LoadedPersona(persona, metadata);
    }

    private static Persona ToPersona(PersonaDocument document, string filePath)
    {
        var id = Require(document.Id, filePath, "id");
        var displayName = Require(document.DisplayName, filePath, "displayName");
        var description = Require(document.Description, filePath, "description");

        if (document.Speech is null)
        {
            throw new PersonaConfigurationException($"Persona file '{filePath}' is missing 'speech'.");
        }

        if (document.Voice is null)
        {
            throw new PersonaConfigurationException($"Persona file '{filePath}' is missing 'voice'.");
        }

        var speechStyle = RequireList(document.Speech.Style, filePath, "speech.style");
        var voiceProvider = Require(document.Voice.Provider, filePath, "voice.provider");
        var voiceId = Require(document.Voice.VoiceId, filePath, "voice.voiceId");
        var voiceStyle = RequireList(document.Voice.Style, filePath, "voice.style");

        return new Persona(
            id,
            displayName,
            description,
            new PersonaSpeechProfile(speechStyle),
            new PersonaVoiceProfile(voiceProvider, voiceId, voiceStyle),
            NormalizeOptional(document.PreviewText));
    }

    private static PersonaVoiceAssets? ToVoiceAssets(JsonElement? assetsElement, string filePath)
    {
        if (assetsElement is null || assetsElement.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (assetsElement.Value.ValueKind != JsonValueKind.Object)
        {
            throw new PersonaConfigurationException($"Persona file '{filePath}' has non-object 'voice.assets'.");
        }

        string? speakerSample = null;
        string? referenceAudio = null;
        string? modelPath = null;
        string? speakerEmbedding = null;
        var providerSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in assetsElement.Value.EnumerateObject())
        {
            switch (property.Name)
            {
                case "speakerSample":
                    speakerSample = ResolveAssetPath(ReadAssetPath(property, filePath), filePath);
                    break;
                case "referenceAudio":
                    referenceAudio = ResolveAssetPath(ReadAssetPath(property, filePath), filePath);
                    break;
                case "modelPath":
                    modelPath = ResolveAssetPath(ReadAssetPath(property, filePath), filePath);
                    break;
                case "speakerEmbedding":
                    speakerEmbedding = ResolveAssetPath(ReadAssetPath(property, filePath), filePath);
                    break;
                case "providerSettings":
                    ReadProviderSettings(property, providerSettings, filePath);
                    break;
                default:
                    throw new PersonaConfigurationException(
                        $"Persona file '{filePath}' has provider-specific voice asset field '{property.Name}'. Put provider-specific values under 'voice.assets.providerSettings'.");
            }
        }

        return new PersonaVoiceAssets(
            speakerSample,
            referenceAudio,
            modelPath,
            speakerEmbedding,
            providerSettings);
    }

    private static void ValidateRawDocument(JsonElement root, PersonaSourceKind sourceKind, string filePath)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new PersonaConfigurationException($"Persona file '{filePath}' must contain a JSON object.");
        }

        if (!root.TryGetProperty("voice", out var voice) || voice.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in voice.EnumerateObject())
        {
            var allowed = property.Name is "provider" or "voiceId" or "style" or "assets";
            if (!allowed)
            {
                throw new PersonaConfigurationException(
                    $"Persona file '{filePath}' has provider-specific voice field '{property.Name}'. Put provider-specific values under 'voice.assets.providerSettings'.");
            }
        }

        if (sourceKind == PersonaSourceKind.Committed)
        {
            ValidateCommittedVoice(root, voice, filePath);
        }
    }

    private static void ValidateCommittedVoice(JsonElement root, JsonElement voice, string filePath)
    {
        if (voice.TryGetProperty("assets", out var assets)
            && assets.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            throw new PersonaConfigurationException(
                $"Committed persona file '{filePath}' must not contain 'voice.assets'. Runtime voice assets belong in local personas.");
        }

        if (voice.TryGetProperty("voiceId", out var voiceId)
            && voiceId.ValueKind == JsonValueKind.String
            && IsPathLike(voiceId.GetString()))
        {
            throw new PersonaConfigurationException(
                $"Committed persona file '{filePath}' must not use local paths for 'voice.voiceId'.");
        }

        RejectSecretOrRuntimeFields(root, filePath);
    }

    private static void RejectSecretOrRuntimeFields(JsonElement element, string filePath)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var normalized = property.Name.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
                if (normalized.Contains("apikey", StringComparison.Ordinal)
                    || normalized.Contains("secret", StringComparison.Ordinal)
                    || normalized.Contains("password", StringComparison.Ordinal)
                    || normalized.Contains("token", StringComparison.Ordinal)
                    || normalized.EndsWith("path", StringComparison.Ordinal)
                    || normalized.Contains("sample", StringComparison.Ordinal)
                    || normalized.Contains("embedding", StringComparison.Ordinal)
                    || normalized.Contains("referenceaudio", StringComparison.Ordinal))
                {
                    throw new PersonaConfigurationException(
                        $"Committed persona file '{filePath}' contains runtime-only or secret-like field '{property.Name}'.");
                }

                RejectSecretOrRuntimeFields(property.Value, filePath);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RejectSecretOrRuntimeFields(item, filePath);
            }
        }
    }

    private static string ReadAssetPath(JsonProperty property, string filePath)
    {
        if (property.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.Value.GetString()))
        {
            throw new PersonaConfigurationException(
                $"Persona file '{filePath}' has invalid 'voice.assets.{property.Name}'. Asset paths must be strings.");
        }

        return property.Value.GetString()!;
    }

    private static void ReadProviderSettings(
        JsonProperty property,
        IDictionary<string, string> providerSettings,
        string filePath)
    {
        if (property.Value.ValueKind != JsonValueKind.Object)
        {
            throw new PersonaConfigurationException(
                $"Persona file '{filePath}' has non-object 'voice.assets.providerSettings'.");
        }

        foreach (var setting in property.Value.EnumerateObject())
        {
            providerSettings[setting.Name] = setting.Value.GetRawText();
        }
    }

    private static string ResolveAssetPath(string value, string filePath)
    {
        if (Path.IsPathFullyQualified(value))
        {
            return Path.GetFullPath(value);
        }

        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath))
            ?? Directory.GetCurrentDirectory();

        return Path.GetFullPath(Path.Combine(baseDirectory, value));
    }

    private static bool IsPathLike(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && (Path.IsPathFullyQualified(value)
                || value.Contains('\\', StringComparison.Ordinal)
                || value.Contains('/', StringComparison.Ordinal));
    }

    private static string Require(string? value, string filePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PersonaConfigurationException($"Persona file '{filePath}' is missing '{fieldName}'.");
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> RequireList(IReadOnlyList<string>? values, string filePath, string fieldName)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new PersonaConfigurationException($"Persona file '{filePath}' must contain non-empty '{fieldName}' entries.");
        }

        return values.Select(value => value.Trim()).ToArray();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record LoadedPersona(Persona Persona, PersonaRuntimeMetadata Metadata);

    private sealed class PersonaDocument
    {
        public string? Id { get; init; }

        public string? DisplayName { get; init; }

        public string? Description { get; init; }

        public string? PreviewText { get; init; }

        public PersonaSpeechDocument? Speech { get; init; }

        public PersonaVoiceDocument? Voice { get; init; }
    }

    private sealed class PersonaSpeechDocument
    {
        public IReadOnlyList<string>? Style { get; init; }
    }

    private sealed class PersonaVoiceDocument
    {
        public string? Provider { get; init; }

        public string? VoiceId { get; init; }

        public IReadOnlyList<string>? Style { get; init; }

        public JsonElement? Assets { get; init; }
    }
}
