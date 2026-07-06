namespace Orpheus.Adapters.Personas;

public enum PersonaSourceKind
{
    Committed,
    Local
}

public sealed record PersonaSourceInfo(
    PersonaSourceKind Kind,
    string Path);

public sealed record PersonaVoiceAssets(
    string? SpeakerSample,
    string? ReferenceAudio,
    string? ModelPath,
    string? SpeakerEmbedding,
    IReadOnlyDictionary<string, string> ProviderSettings);

public sealed record PersonaRuntimeMetadata(
    string PersonaId,
    PersonaSourceInfo Source,
    PersonaVoiceAssets? VoiceAssets);

public interface IPersonaRuntimeMetadataResolver
{
    PersonaRuntimeMetadata? GetRuntimeMetadata(string personaId);
}

public static class PersonaRuntimeMetadataResolverExtensions
{
    public static PersonaRuntimeMetadata RequireRuntimeMetadata(
        this IPersonaRuntimeMetadataResolver resolver,
        string personaId,
        string providerName)
    {
        return resolver.GetRuntimeMetadata(personaId)
            ?? throw new PersonaConfigurationException(
                $"Provider '{providerName}' requires runtime metadata for persona '{personaId}', but none was resolved.");
    }
}
