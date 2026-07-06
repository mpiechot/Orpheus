namespace Orpheus.Core.Abstractions;

public interface ILastSpeechTextStore
{
    Task StoreAsync(string personaId, string originalText, CancellationToken cancellationToken = default);

    Task<string?> GetLastOriginalTextAsync(string personaId, CancellationToken cancellationToken = default);
}
