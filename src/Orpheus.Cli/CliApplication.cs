using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.State;
using Orpheus.Adapters.Transformation;
using Orpheus.Adapters.Voice;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Exceptions;
using Orpheus.Core.Models;
using Orpheus.Core.Services;

namespace Orpheus.Cli;

public static class CliApplication
{
    private const string DefaultProviderName = "deterministic-wav";

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        string? startDirectory = null,
        string? globalPreviewText = null)
    {
        if (args.Length == 0)
        {
            await WriteUsageAsync(standardError);
            return 1;
        }

        var runtime = CreateRuntime(startDirectory);

        try
        {
            if (string.Equals(args[0], "voice", StringComparison.OrdinalIgnoreCase))
            {
                return await RunVoiceCommandAsync(
                    args,
                    runtime,
                    standardOutput,
                    standardError,
                    globalPreviewText ?? Environment.GetEnvironmentVariable("ORPHEUS_PREVIEW_TEXT"));
            }

            return await RunSpeakCommandAsync(args, runtime, standardOutput, standardError);
        }
        catch (SpeechRequestValidationException exception)
        {
            await standardError.WriteLineAsync(exception.Message);
            return 1;
        }
        catch (PersonaConfigurationException exception)
        {
            await standardError.WriteLineAsync(exception.Message);
            return 1;
        }
        catch (PersonaNotFoundException exception)
        {
            await standardError.WriteLineAsync(exception.Message);
            return 2;
        }
    }

    private static async Task<int> RunSpeakCommandAsync(
        string[] args,
        CliRuntime runtime,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        if (args.Length < 2)
        {
            await WriteUsageAsync(standardError);
            return 1;
        }

        var personaId = args[0];
        var text = string.Join(' ', args.Skip(1));
        var result = await runtime.SpeechEngine.SpeakAsync(new SpeechRequest(personaId, text));

        await standardOutput.WriteLineAsync(result.Text);
        await standardOutput.WriteLineAsync(result.Audio.Uri);
        return 0;
    }

    private static async Task<int> RunVoiceCommandAsync(
        string[] args,
        CliRuntime runtime,
        TextWriter standardOutput,
        TextWriter standardError,
        string? globalPreviewText)
    {
        if (args.Length < 3)
        {
            await WriteUsageAsync(standardError);
            return 1;
        }

        var command = args[1].Trim().ToLowerInvariant();
        var personaId = args[2];
        var persona = await runtime.PersonaRepository.GetByIdAsync(personaId)
            ?? throw new PersonaNotFoundException(personaId);

        return command switch
        {
            "status" => await RunVoiceStatusAsync(persona, runtime, standardOutput),
            "regenerate" => await RunVoiceRegenerateAsync(
                args,
                persona,
                runtime,
                standardOutput,
                globalPreviewText),
            "accept" => await RunVoiceAcceptAsync(args, persona, runtime, standardOutput),
            "reject" => await RunVoiceRejectAsync(args, persona, runtime, standardOutput),
            _ => await WriteUnknownVoiceCommandAsync(command, standardError)
        };
    }

    private static async Task<int> RunVoiceStatusAsync(
        Persona persona,
        CliRuntime runtime,
        TextWriter standardOutput)
    {
        var status = await runtime.VoiceIdentityStore.GetStatusAsync(persona, runtime.ProviderName);
        var metadata = runtime.RuntimeMetadataResolver.GetRuntimeMetadata(persona.Id);

        await standardOutput.WriteLineAsync($"persona: {persona.Id}");
        await standardOutput.WriteLineAsync($"source: {metadata?.Source.Kind.ToString().ToLowerInvariant() ?? "unknown"}");
        await standardOutput.WriteLineAsync($"provider: {runtime.ProviderName}");
        await standardOutput.WriteLineAsync($"active: {status.Active?.Id ?? "none"}");
        await standardOutput.WriteLineAsync($"candidates: {status.Candidates.Count}");
        await standardOutput.WriteLineAsync($"rejected: {status.Rejected.Count}");
        await standardOutput.WriteLineAsync($"stale: {(status.ActiveIsStale ? "yes" : "no")}");

        if (status.Warning is not null)
        {
            await standardOutput.WriteLineAsync($"warning: {status.Warning}");
        }

        return 0;
    }

    private static async Task<int> RunVoiceRegenerateAsync(
        string[] args,
        Persona persona,
        CliRuntime runtime,
        TextWriter standardOutput,
        string? globalPreviewText)
    {
        var explicitText = ReadTextOption(args.Skip(3).ToArray());
        var preview = await SelectPreviewTextAsync(
            persona,
            explicitText,
            runtime.LastSpeechTextStore,
            globalPreviewText);
        var candidate = await runtime.VoiceIdentityStore.CreateCandidateAsync(
            persona,
            runtime.ProviderName,
            preview.Text);
        var transformed = await runtime.PersonaTransformer.TransformAsync(persona, preview.Text);
        var audio = await runtime.BaseTextToSpeechProvider.SynthesizeAsync(
            new SpeechSynthesisRequest(persona, transformed.Text, candidate.Fingerprint));
        var updated = await runtime.VoiceIdentityStore.SetCandidatePreviewAudioAsync(
            persona.Id,
            runtime.ProviderName,
            candidate.Id,
            audio.Uri);

        await standardOutput.WriteLineAsync($"candidate: {updated.Id}");
        await standardOutput.WriteLineAsync($"preview-source: {preview.Source}");
        await standardOutput.WriteLineAsync($"preview-audio: {updated.PreviewAudioUri}");
        return 0;
    }

    private static async Task<int> RunVoiceAcceptAsync(
        string[] args,
        Persona persona,
        CliRuntime runtime,
        TextWriter standardOutput)
    {
        if (args.Length < 4)
        {
            throw new SpeechRequestValidationException("Voice candidate id is required.");
        }

        var accepted = await runtime.VoiceIdentityStore.AcceptAsync(
            persona.Id,
            runtime.ProviderName,
            args[3]);

        await standardOutput.WriteLineAsync($"active: {accepted.Id}");
        return 0;
    }

    private static async Task<int> RunVoiceRejectAsync(
        string[] args,
        Persona persona,
        CliRuntime runtime,
        TextWriter standardOutput)
    {
        if (args.Length < 4)
        {
            throw new SpeechRequestValidationException("Voice candidate id is required.");
        }

        var rejected = await runtime.VoiceIdentityStore.RejectAsync(
            persona.Id,
            runtime.ProviderName,
            args[3]);

        await standardOutput.WriteLineAsync($"rejected: {rejected.Id}");
        return 0;
    }

    private static async Task<PreviewTextSelection> SelectPreviewTextAsync(
        Persona persona,
        string? explicitText,
        ILastSpeechTextStore lastSpeechTextStore,
        string? globalPreviewText)
    {
        if (!string.IsNullOrWhiteSpace(explicitText))
        {
            return new PreviewTextSelection(explicitText.Trim(), "explicit");
        }

        var lastOriginalText = await lastSpeechTextStore.GetLastOriginalTextAsync(persona.Id);
        if (!string.IsNullOrWhiteSpace(lastOriginalText))
        {
            return new PreviewTextSelection(lastOriginalText.Trim(), "last-original-text");
        }

        if (!string.IsNullOrWhiteSpace(persona.PreviewText))
        {
            return new PreviewTextSelection(persona.PreviewText.Trim(), "persona-preview-text");
        }

        if (!string.IsNullOrWhiteSpace(globalPreviewText))
        {
            return new PreviewTextSelection(globalPreviewText.Trim(), "global-preview-text");
        }

        throw new SpeechRequestValidationException(
            $"No preview text is available for persona '{persona.Id}'. Provide --text, speak once, configure persona previewText, or set ORPHEUS_PREVIEW_TEXT.");
    }

    private static string? ReadTextOption(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (!string.Equals(args[index], "--text", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Count)
            {
                throw new SpeechRequestValidationException("Voice preview text is required after --text.");
            }

            return string.Join(' ', args.Skip(index + 1));
        }

        return null;
    }

    private static async Task<int> WriteUnknownVoiceCommandAsync(string command, TextWriter standardError)
    {
        await standardError.WriteLineAsync($"Unknown voice command '{command}'.");
        await WriteUsageAsync(standardError);
        return 1;
    }

    private static async Task WriteUsageAsync(TextWriter standardError)
    {
        await standardError.WriteLineAsync("Usage: orpheus <persona-id> <text>");
        await standardError.WriteLineAsync("Usage: orpheus voice status <persona-id>");
        await standardError.WriteLineAsync("Usage: orpheus voice regenerate <persona-id> [--text \"...\"]");
        await standardError.WriteLineAsync("Usage: orpheus voice accept <persona-id> <candidate-id>");
        await standardError.WriteLineAsync("Usage: orpheus voice reject <persona-id> <candidate-id>");
    }

    private static CliRuntime CreateRuntime(string? startDirectory)
    {
        var filePersonaRepository = PersonaRepositoryFactory.CreateDefault(startDirectory);
        var audioDirectory = PersonaRepositoryFactory.ResolveRuntimeDirectory("audio", startDirectory);
        var stateDirectory = PersonaRepositoryFactory.ResolveRuntimeDirectory("state", startDirectory);
        var voicesDirectory = PersonaRepositoryFactory.ResolveRuntimeDirectory("voices", startDirectory);
        var lastSpeechTextStore = new FileLastSpeechTextStore(new FileLastSpeechTextStoreOptions(stateDirectory));
        var voiceIdentityStore = new FileVoiceIdentityStore(new FileVoiceIdentityStoreOptions(voicesDirectory, filePersonaRepository));
        var baseTextToSpeechProvider = TextToSpeechProviderFactory.Create(DefaultProviderName, audioDirectory);
        var textToSpeechProvider = new VoiceIdentityTextToSpeechProvider(
            baseTextToSpeechProvider,
            voiceIdentityStore,
            DefaultProviderName);
        IPersonaTransformer personaTransformer = new StubPersonaTransformer();
        var speechEngine = new SpeechEngine(
            filePersonaRepository,
            personaTransformer,
            textToSpeechProvider,
            lastSpeechTextStore);

        return new CliRuntime(
            filePersonaRepository,
            filePersonaRepository,
            personaTransformer,
            baseTextToSpeechProvider,
            voiceIdentityStore,
            lastSpeechTextStore,
            speechEngine,
            DefaultProviderName);
    }

    private sealed record CliRuntime(
        IPersonaRepository PersonaRepository,
        IPersonaRuntimeMetadataResolver RuntimeMetadataResolver,
        IPersonaTransformer PersonaTransformer,
        ITextToSpeechProvider BaseTextToSpeechProvider,
        IVoiceIdentityStore VoiceIdentityStore,
        ILastSpeechTextStore LastSpeechTextStore,
        SpeechEngine SpeechEngine,
        string ProviderName);

    private sealed record PreviewTextSelection(string Text, string Source);
}
