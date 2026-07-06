using System.Diagnostics;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Speech;

public sealed record WindowsSapiTextToSpeechProviderOptions(
    string OutputDirectory,
    string? VoiceName = null,
    TimeSpan? Timeout = null);

public sealed class WindowsSapiTextToSpeechProvider : ITextToSpeechProvider
{
    private const string ProviderName = "windows-sapi";
    private const string PowerShellScript = """
        param(
          [string] $Text,
          [string] $OutputPath,
          [string] $VoiceName
        )
        $ErrorActionPreference = 'Stop'
        Add-Type -AssemblyName System.Speech
        $synthesizer = [System.Speech.Synthesis.SpeechSynthesizer]::new()
        try {
          if (-not [string]::IsNullOrWhiteSpace($VoiceName)) {
            $synthesizer.SelectVoice($VoiceName)
          }
          $synthesizer.SetOutputToWaveFile($OutputPath)
          $synthesizer.Speak($Text)
        }
        finally {
          $synthesizer.Dispose()
        }
        """;

    private readonly WindowsSapiTextToSpeechProviderOptions _options;

    public WindowsSapiTextToSpeechProvider(WindowsSapiTextToSpeechProviderOptions options)
    {
        _options = options;
    }

    public async Task<AudioResult> SynthesizeAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows SAPI text-to-speech requires Windows.");
        }

        Directory.CreateDirectory(_options.OutputDirectory);
        var outputPath = SpeechAudioFileNamer.GetOutputPath(_options.OutputDirectory, request, ProviderName, "wav");

        if (!File.Exists(outputPath))
        {
            await RunSapiAsync(request.Text, outputPath, cancellationToken);
        }

        return new AudioResult(
            new Uri(outputPath).AbsoluteUri,
            ProviderName,
            string.IsNullOrWhiteSpace(_options.VoiceName) ? "windows-sapi-default" : _options.VoiceName,
            "audio/wav");
    }

    private async Task RunSapiAsync(
        string text,
        string outputPath,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        var scriptPath = GetScriptPath();

        process.StartInfo.FileName = "powershell.exe";
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(scriptPath);
        process.StartInfo.ArgumentList.Add("-Text");
        process.StartInfo.ArgumentList.Add(text);
        process.StartInfo.ArgumentList.Add("-OutputPath");
        process.StartInfo.ArgumentList.Add(outputPath);
        if (!string.IsNullOrWhiteSpace(_options.VoiceName))
        {
            process.StartInfo.ArgumentList.Add("-VoiceName");
            process.StartInfo.ArgumentList.Add(_options.VoiceName);
        }
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        var timeout = _options.Timeout ?? TimeSpan.FromSeconds(30);
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
            throw new TimeoutException($"Windows SAPI synthesis exceeded the configured timeout of {timeout.TotalSeconds:0} seconds.");
        }

        var standardError = await process.StandardError.ReadToEndAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Windows SAPI synthesis failed with exit code {process.ExitCode}: {standardError.Trim()}");
        }

        if (!File.Exists(outputPath) || new FileInfo(outputPath).Length <= 44)
        {
            throw new InvalidOperationException(
                $"Windows SAPI synthesis did not produce a valid WAV file at '{outputPath}'.");
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

    private string GetScriptPath()
    {
        var scriptPath = Path.Combine(_options.OutputDirectory, "orpheus-windows-sapi-synth.ps1");
        if (!File.Exists(scriptPath))
        {
            File.WriteAllText(scriptPath, PowerShellScript);
        }

        return scriptPath;
    }
}
