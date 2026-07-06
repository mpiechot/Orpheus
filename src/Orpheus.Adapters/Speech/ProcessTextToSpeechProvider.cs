using System.Diagnostics;
using System.Text.Json;
using Orpheus.Adapters.Personas;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Speech;

public sealed record ProcessTextToSpeechProviderOptions(
    string ProviderName,
    string Command,
    IReadOnlyList<string> Arguments,
    string OutputDirectory,
    string OutputFormat = "wav",
    string ContentType = "audio/wav",
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null,
    bool RequireReferenceAudio = false,
    bool RequireSpeakerSample = false,
    bool RequireModelPath = false,
    bool RequireSpeakerEmbedding = false);

public sealed class ProcessTextToSpeechProvider : ITextToSpeechProvider
{
    private readonly ProcessTextToSpeechProviderOptions _options;
    private readonly IPersonaRuntimeMetadataResolver _runtimeMetadataResolver;

    public ProcessTextToSpeechProvider(
        ProcessTextToSpeechProviderOptions options,
        IPersonaRuntimeMetadataResolver runtimeMetadataResolver)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderName))
        {
            throw new ArgumentException("Process provider name is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Command))
        {
            throw new ArgumentException("Process provider command is required.", nameof(options));
        }

        if (options.Arguments.Count == 0)
        {
            throw new ArgumentException("Process provider arguments are required.", nameof(options));
        }

        _options = options;
        _runtimeMetadataResolver = runtimeMetadataResolver;
    }

    public async Task<AudioResult> SynthesizeAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.OutputDirectory);
        var outputPath = SpeechAudioFileNamer.GetOutputPath(
            _options.OutputDirectory,
            request,
            _options.ProviderName,
            _options.OutputFormat);
        EnsurePathInsideDirectory(outputPath, _options.OutputDirectory);

        if (!File.Exists(outputPath))
        {
            var textFilePath = GetTextFilePath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(textFilePath)!);
            await File.WriteAllTextAsync(textFilePath, request.Text, cancellationToken);

            var metadata = _runtimeMetadataResolver.GetRuntimeMetadata(request.Persona.Id);
            var arguments = BuildArguments(request, metadata, textFilePath, outputPath);
            await RunProcessAsync(arguments, outputPath, cancellationToken);
        }

        return new AudioResult(
            new Uri(outputPath).AbsoluteUri,
            _options.ProviderName,
            request.Persona.Voice.VoiceId,
            _options.ContentType);
    }

    private IReadOnlyList<string> BuildArguments(
        SpeechSynthesisRequest request,
        PersonaRuntimeMetadata? metadata,
        string textFilePath,
        string outputPath)
    {
        var assets = metadata?.VoiceAssets;
        ValidateRequiredAssets(request.Persona.Id, assets);
        ValidateUnsupportedAssets(request.Persona.Id, assets);

        return _options.Arguments
            .Select(argument => ReplaceTokens(argument, request, assets, textFilePath, outputPath))
            .ToArray();
    }

    private void ValidateRequiredAssets(string personaId, PersonaVoiceAssets? assets)
    {
        if (_options.RequireReferenceAudio)
        {
            RequireExistingAsset(personaId, "referenceAudio", assets?.ReferenceAudio);
        }

        if (_options.RequireSpeakerSample)
        {
            RequireExistingAsset(personaId, "speakerSample", assets?.SpeakerSample);
        }

        if (_options.RequireModelPath)
        {
            RequireExistingAsset(personaId, "modelPath", assets?.ModelPath);
        }

        if (_options.RequireSpeakerEmbedding)
        {
            RequireExistingAsset(personaId, "speakerEmbedding", assets?.SpeakerEmbedding);
        }
    }

    private void ValidateUnsupportedAssets(string personaId, PersonaVoiceAssets? assets)
    {
        if (assets is null)
        {
            return;
        }

        RejectUnusedAsset(personaId, "speakerSample", assets.SpeakerSample, "{speakerSample}");
        RejectUnusedAsset(personaId, "referenceAudio", assets.ReferenceAudio, "{referenceAudio}");
        RejectUnusedAsset(personaId, "modelPath", assets.ModelPath, "{modelPath}");
        RejectUnusedAsset(personaId, "speakerEmbedding", assets.SpeakerEmbedding, "{speakerEmbedding}");

        foreach (var setting in assets.ProviderSettings)
        {
            RejectUnusedAsset(
                personaId,
                $"providerSettings.{setting.Key}",
                setting.Value,
                $"{{setting:{setting.Key}}}");
        }
    }

    private void RejectUnusedAsset(
        string personaId,
        string fieldName,
        string? value,
        string token)
    {
        if (string.IsNullOrWhiteSpace(value) || ArgumentTemplatesContain(token))
        {
            return;
        }

        throw new PersonaConfigurationException(
            $"Provider '{_options.ProviderName}' does not support voice.assets.{fieldName} for persona '{personaId}'.");
    }

    private bool ArgumentTemplatesContain(string token)
    {
        return _options.Arguments.Any(argument => argument.Contains(token, StringComparison.Ordinal));
    }

    private string ReplaceTokens(
        string argument,
        SpeechSynthesisRequest request,
        PersonaVoiceAssets? assets,
        string textFilePath,
        string outputPath)
    {
        var result = argument
            .Replace("{textFile}", textFilePath, StringComparison.Ordinal)
            .Replace("{outputFile}", outputPath, StringComparison.Ordinal)
            .Replace("{personaId}", request.Persona.Id, StringComparison.Ordinal)
            .Replace("{voiceId}", request.Persona.Voice.VoiceId, StringComparison.Ordinal)
            .Replace("{voiceIdentityKey}", request.VoiceIdentityKey ?? string.Empty, StringComparison.Ordinal);

        result = ReplaceAssetToken(result, request.Persona.Id, "referenceAudio", "{referenceAudio}", assets?.ReferenceAudio);
        result = ReplaceAssetToken(result, request.Persona.Id, "speakerSample", "{speakerSample}", assets?.SpeakerSample);
        result = ReplaceAssetToken(result, request.Persona.Id, "modelPath", "{modelPath}", assets?.ModelPath);
        result = ReplaceAssetToken(result, request.Persona.Id, "speakerEmbedding", "{speakerEmbedding}", assets?.SpeakerEmbedding);

        if (assets?.ProviderSettings is null)
        {
            return result;
        }

        foreach (var setting in assets.ProviderSettings)
        {
            result = result.Replace(
                $"{{setting:{setting.Key}}}",
                NormalizeProviderSetting(setting.Value),
                StringComparison.Ordinal);
        }

        return result;
    }

    private string ReplaceAssetToken(
        string argument,
        string personaId,
        string fieldName,
        string token,
        string? path)
    {
        return argument.Contains(token, StringComparison.Ordinal)
            ? argument.Replace(token, RequireExistingAsset(personaId, fieldName, path), StringComparison.Ordinal)
            : argument;
    }

    private string RequireExistingAsset(string personaId, string fieldName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new PersonaConfigurationException(
                $"Provider '{_options.ProviderName}' requires voice.assets.{fieldName} for persona '{personaId}'.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Provider '{_options.ProviderName}' requires existing voice.assets.{fieldName} for persona '{personaId}'.",
                fullPath);
        }

        return fullPath;
    }

    private static string NormalizeProviderSetting(string rawValue)
    {
        try
        {
            using var document = JsonDocument.Parse(rawValue);
            return document.RootElement.ValueKind == JsonValueKind.String
                ? document.RootElement.GetString() ?? string.Empty
                : rawValue;
        }
        catch (JsonException)
        {
            return rawValue;
        }
    }

    private async Task RunProcessAsync(
        IReadOnlyList<string> arguments,
        string outputPath,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo.FileName = _options.Command;
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(_options.WorkingDirectory))
        {
            process.StartInfo.WorkingDirectory = _options.WorkingDirectory;
        }

        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException(
                $"Provider '{_options.ProviderName}' could not start command '{_options.Command}'.",
                exception);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var timeout = _options.Timeout ?? TimeSpan.FromSeconds(60);
        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);

        try
        {
            await process.WaitForExitAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException(
                $"Provider '{_options.ProviderName}' exceeded the configured timeout of {timeout.TotalSeconds:0} seconds.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(standardError)
                ? standardOutput.Trim()
                : standardError.Trim();
            throw new InvalidOperationException(
                $"Provider '{_options.ProviderName}' failed with exit code {process.ExitCode}: {details}");
        }

        ValidateOutputFile(outputPath);
    }

    private void ValidateOutputFile(string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException(
                $"Provider '{_options.ProviderName}' did not create output file '{outputPath}'.");
        }

        var fileInfo = new FileInfo(outputPath);
        var minimumLength = string.Equals(_options.OutputFormat.TrimStart('.'), "wav", StringComparison.OrdinalIgnoreCase)
            ? 44
            : 0;
        if (fileInfo.Length <= minimumLength)
        {
            throw new InvalidOperationException(
                $"Provider '{_options.ProviderName}' did not create a valid {_options.OutputFormat.ToUpperInvariant()} file at '{outputPath}'.");
        }
    }

    private static string GetTextFilePath(string outputPath)
    {
        var directory = Path.Combine(Path.GetDirectoryName(outputPath)!, "inputs");
        var fileName = Path.GetFileNameWithoutExtension(outputPath) + ".txt";
        return Path.Combine(directory, fileName);
    }

    private static void EnsurePathInsideDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar))
        {
            fullDirectory += Path.DirectorySeparatorChar;
        }

        if (!fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Output path '{path}' must stay inside output directory '{directory}'.");
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
