using Orpheus.Core.Models;

namespace Orpheus.Core.Abstractions;

public interface IAudioCache
{
    Task<AudioResult?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);

    Task StoreAsync(string cacheKey, AudioResult audio, CancellationToken cancellationToken = default);
}
