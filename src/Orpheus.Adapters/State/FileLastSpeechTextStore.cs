using System.Text.Json;
using System.Text.RegularExpressions;
using Orpheus.Core.Abstractions;

namespace Orpheus.Adapters.State;

public sealed record FileLastSpeechTextStoreOptions(
    string StateDirectory,
    bool Enabled = true);

public sealed partial class FileLastSpeechTextStore : ILastSpeechTextStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly FileLastSpeechTextStoreOptions _options;

    public FileLastSpeechTextStore(FileLastSpeechTextStoreOptions options)
    {
        _options = options;
    }

    public async Task StoreAsync(
        string personaId,
        string originalText,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        Directory.CreateDirectory(_options.StateDirectory);
        TryApplyUserOnlyPermissions(_options.StateDirectory);

        var state = new LastSpeechTextState(personaId, originalText, DateTimeOffset.UtcNow);
        var filePath = GetFilePath(personaId);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(state, JsonOptions), cancellationToken);
        TryApplyUserOnlyPermissions(filePath);
    }

    public async Task<string?> GetLastOriginalTextAsync(
        string personaId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var filePath = GetFilePath(personaId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        var state = await JsonSerializer.DeserializeAsync<LastSpeechTextState>(stream, cancellationToken: cancellationToken);
        return state?.OriginalText;
    }

    private string GetFilePath(string personaId)
    {
        var normalized = SafeFileNameRegex().Replace(personaId.Trim().ToLowerInvariant(), "-").Trim('-');
        var safePersonaId = string.IsNullOrWhiteSpace(normalized)
            ? "persona"
            : normalized;

        return Path.Combine(_options.StateDirectory, $"{safePersonaId}.last-text.json");
    }

    private static void TryApplyUserOnlyPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            if (Directory.Exists(path))
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            else if (File.Exists(path))
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    [GeneratedRegex("[^a-z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex SafeFileNameRegex();

    private sealed record LastSpeechTextState(
        string PersonaId,
        string OriginalText,
        DateTimeOffset StoredAtUtc);
}
