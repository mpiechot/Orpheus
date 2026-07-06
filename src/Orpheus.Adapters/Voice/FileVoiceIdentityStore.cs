using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orpheus.Adapters.Personas;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Voice;

public sealed record FileVoiceIdentityStoreOptions(
    string VoiceDirectory,
    IPersonaRuntimeMetadataResolver? RuntimeMetadataResolver = null);

public sealed class FileVoiceIdentityStore : IVoiceIdentityStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly FileVoiceIdentityStoreOptions _options;

    public FileVoiceIdentityStore(FileVoiceIdentityStoreOptions options)
    {
        _options = options;
    }

    public async Task<VoiceIdentityResolution> GetOrCreateActiveAsync(
        Persona persona,
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(persona.Id, providerName, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var currentFingerprint = CreateFingerprint(persona, providerName);
        var active = document.Identities.FirstOrDefault(identity => identity.State == VoiceIdentityState.Active);

        if (active is not null)
        {
            var stale = !string.Equals(active.Fingerprint, currentFingerprint, StringComparison.Ordinal);
            return new VoiceIdentityResolution(active, stale, stale ? CreateStaleWarning(persona.Id, providerName) : null);
        }

        var created = new VoiceIdentity(
            CreateIdentityId(),
            persona.Id,
            providerName,
            currentFingerprint,
            VoiceIdentityState.Active,
            now,
            now);

        document.Identities.Add(created);
        await WriteDocumentAsync(document, cancellationToken);

        return new VoiceIdentityResolution(created, IsStale: false, Warning: null);
    }

    public async Task<VoiceIdentityStatus> GetStatusAsync(
        Persona persona,
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(persona.Id, providerName, cancellationToken);
        var active = document.Identities.FirstOrDefault(identity => identity.State == VoiceIdentityState.Active);
        var currentFingerprint = CreateFingerprint(persona, providerName);
        var stale = active is not null
            && !string.Equals(active.Fingerprint, currentFingerprint, StringComparison.Ordinal);

        return new VoiceIdentityStatus(
            active,
            document.Identities
                .Where(identity => identity.State == VoiceIdentityState.Candidate)
                .OrderBy(identity => identity.CreatedAt)
                .ToArray(),
            document.Identities
                .Where(identity => identity.State == VoiceIdentityState.Rejected)
                .OrderBy(identity => identity.UpdatedAt)
                .ToArray(),
            stale,
            stale ? CreateStaleWarning(persona.Id, providerName) : null);
    }

    public async Task<VoiceIdentity> CreateCandidateAsync(
        Persona persona,
        string providerName,
        string previewText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(previewText))
        {
            throw new PersonaConfigurationException("Voice candidate preview text is required.");
        }

        var document = await ReadDocumentAsync(persona.Id, providerName, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var candidate = new VoiceIdentity(
            CreateIdentityId(),
            persona.Id,
            providerName,
            CreateFingerprint(persona, providerName),
            VoiceIdentityState.Candidate,
            now,
            now,
            previewText.Trim());

        document.Identities.Add(candidate);
        await WriteDocumentAsync(document, cancellationToken);

        return candidate;
    }

    public async Task<VoiceIdentity> SetCandidatePreviewAudioAsync(
        string personaId,
        string providerName,
        string candidateId,
        string previewAudioUri,
        CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(personaId, providerName, cancellationToken);
        var candidate = FindIdentity(document, candidateId, VoiceIdentityState.Candidate);
        var updated = candidate with
        {
            PreviewAudioUri = previewAudioUri,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        ReplaceIdentity(document, updated);
        await WriteDocumentAsync(document, cancellationToken);

        return updated;
    }

    public async Task<VoiceIdentity> AcceptAsync(
        string personaId,
        string providerName,
        string candidateId,
        CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(personaId, providerName, cancellationToken);
        var candidate = FindIdentity(document, candidateId, VoiceIdentityState.Candidate);
        var now = DateTimeOffset.UtcNow;
        var updated = candidate with
        {
            State = VoiceIdentityState.Active,
            UpdatedAt = now
        };

        for (var index = 0; index < document.Identities.Count; index++)
        {
            var identity = document.Identities[index];
            if (identity.Id == candidate.Id)
            {
                document.Identities[index] = updated;
            }
            else if (identity.State is VoiceIdentityState.Active or VoiceIdentityState.Candidate)
            {
                document.Identities[index] = identity with
                {
                    State = VoiceIdentityState.Rejected,
                    UpdatedAt = now
                };
            }
        }

        await WriteDocumentAsync(document, cancellationToken);
        return updated;
    }

    public async Task<VoiceIdentity> RejectAsync(
        string personaId,
        string providerName,
        string candidateId,
        CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(personaId, providerName, cancellationToken);
        var candidate = FindIdentity(document, candidateId, VoiceIdentityState.Candidate);
        var updated = candidate with
        {
            State = VoiceIdentityState.Rejected,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        ReplaceIdentity(document, updated);
        await WriteDocumentAsync(document, cancellationToken);

        return updated;
    }

    private string CreateFingerprint(Persona persona, string providerName)
    {
        var metadata = _options.RuntimeMetadataResolver?.GetRuntimeMetadata(persona.Id);
        var assets = metadata?.VoiceAssets;
        var hashInput = string.Join(
            '\n',
            providerName.Trim(),
            persona.Id,
            persona.Voice.Provider,
            persona.Voice.VoiceId,
            string.Join('|', persona.Voice.Style),
            AssetFingerprint("speakerSample", assets?.SpeakerSample),
            AssetFingerprint("referenceAudio", assets?.ReferenceAudio),
            AssetFingerprint("modelPath", assets?.ModelPath),
            AssetFingerprint("speakerEmbedding", assets?.SpeakerEmbedding),
            ProviderSettingsFingerprint(assets?.ProviderSettings));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput))).ToLowerInvariant();
    }

    private static string AssetFingerprint(string name, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return $"{name}:";
        }

        var normalized = Path.GetFullPath(path);
        if (!File.Exists(normalized) && !Directory.Exists(normalized))
        {
            return $"{name}:{normalized}:missing";
        }

        if (File.Exists(normalized))
        {
            var file = new FileInfo(normalized);
            return $"{name}:{normalized}:file:{file.Length}:{file.LastWriteTimeUtc.Ticks}";
        }

        var directory = new DirectoryInfo(normalized);
        return $"{name}:{normalized}:directory:{directory.LastWriteTimeUtc.Ticks}";
    }

    private static string ProviderSettingsFingerprint(IReadOnlyDictionary<string, string>? providerSettings)
    {
        if (providerSettings is null || providerSettings.Count == 0)
        {
            return "providerSettings:";
        }

        return string.Join(
            '\n',
            providerSettings
                .OrderBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
                .Select(setting => $"providerSettings:{setting.Key}={setting.Value}"));
    }

    private async Task<VoiceIdentityDocument> ReadDocumentAsync(
        string personaId,
        string providerName,
        CancellationToken cancellationToken)
    {
        var filePath = GetDocumentPath(personaId, providerName);
        if (!File.Exists(filePath))
        {
            return new VoiceIdentityDocument(personaId, providerName, filePath, []);
        }

        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<VoiceIdentityDocument>(
            stream,
            JsonOptions,
            cancellationToken);

        return document ?? new VoiceIdentityDocument(personaId, providerName, filePath, []);
    }

    private async Task WriteDocumentAsync(
        VoiceIdentityDocument document,
        CancellationToken cancellationToken)
    {
        var filePath = GetDocumentPath(document.PersonaId, document.Provider);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document with { Path = filePath }, JsonOptions, cancellationToken);
    }

    private string GetDocumentPath(string personaId, string providerName)
    {
        var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes($"{personaId}\n{providerName}")))
            .ToLowerInvariant();

        return Path.Combine(_options.VoiceDirectory, $"{hash[..16]}.json");
    }

    private static VoiceIdentity FindIdentity(
        VoiceIdentityDocument document,
        string identityId,
        VoiceIdentityState requiredState)
    {
        var identity = document.Identities.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, identityId, StringComparison.OrdinalIgnoreCase));

        if (identity is null)
        {
            throw new PersonaConfigurationException(
                $"Voice identity '{identityId}' was not found for persona '{document.PersonaId}'.");
        }

        if (identity.State != requiredState)
        {
            throw new PersonaConfigurationException(
                $"Voice identity '{identityId}' for persona '{document.PersonaId}' is '{identity.State}', not '{requiredState}'.");
        }

        return identity;
    }

    private static void ReplaceIdentity(VoiceIdentityDocument document, VoiceIdentity updated)
    {
        var index = document.Identities.FindIndex(identity => identity.Id == updated.Id);
        if (index < 0)
        {
            throw new PersonaConfigurationException(
                $"Voice identity '{updated.Id}' was not found for persona '{document.PersonaId}'.");
        }

        document.Identities[index] = updated;
    }

    private static string CreateIdentityId()
    {
        return $"voice-{Guid.NewGuid():N}"[..18];
    }

    private static string CreateStaleWarning(string personaId, string providerName)
    {
        return $"Active voice for persona '{personaId}' and provider '{providerName}' is stale.";
    }

    private sealed record VoiceIdentityDocument(
        string PersonaId,
        string Provider,
        string Path,
        List<VoiceIdentity> Identities);
}
