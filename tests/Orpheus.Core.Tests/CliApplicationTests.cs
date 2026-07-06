using Orpheus.Cli;

namespace Orpheus.Core.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task Speak_command_creates_active_voice_identity()
    {
        using var workspace = TestWorkspace.Create();
        workspace.WritePersona("guide", previewText: "Preview from persona.");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["guide", "Continue", "straight."],
            output,
            error,
            workspace.Root);
        using var statusOutput = new StringWriter();

        var statusExitCode = await CliApplication.RunAsync(
            ["voice", "status", "guide"],
            statusOutput,
            error,
            workspace.Root);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, statusExitCode);
        Assert.Contains("source: committed", statusOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("active: voice-", statusOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("stale: no", statusOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Voice_regenerate_accept_and_reject_commands_manage_candidates()
    {
        using var workspace = TestWorkspace.Create();
        workspace.WritePersona("guide", previewText: "Preview from persona.");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var regenerateExitCode = await CliApplication.RunAsync(
            ["voice", "regenerate", "guide", "--text", "Use this preview."],
            output,
            error,
            workspace.Root);
        var candidateId = output.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("candidate:", StringComparison.Ordinal))
            .Replace("candidate:", string.Empty, StringComparison.Ordinal)
            .Trim();
        using var acceptOutput = new StringWriter();

        var acceptExitCode = await CliApplication.RunAsync(
            ["voice", "accept", "guide", candidateId],
            acceptOutput,
            error,
            workspace.Root);
        using var secondRegenerateOutput = new StringWriter();
        var secondRegenerateExitCode = await CliApplication.RunAsync(
            ["voice", "regenerate", "guide", "--text", "Reject this preview."],
            secondRegenerateOutput,
            error,
            workspace.Root);
        var rejectedCandidateId = secondRegenerateOutput.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("candidate:", StringComparison.Ordinal))
            .Replace("candidate:", string.Empty, StringComparison.Ordinal)
            .Trim();
        using var rejectOutput = new StringWriter();

        var rejectExitCode = await CliApplication.RunAsync(
            ["voice", "reject", "guide", rejectedCandidateId],
            rejectOutput,
            error,
            workspace.Root);

        Assert.Equal(0, regenerateExitCode);
        Assert.Contains("preview-source: explicit", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("preview-audio: file://", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(0, acceptExitCode);
        Assert.Contains($"active: {candidateId}", acceptOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(0, secondRegenerateExitCode);
        Assert.Equal(0, rejectExitCode);
        Assert.Contains($"rejected: {rejectedCandidateId}", rejectOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Voice_regenerate_uses_last_original_text_before_persona_preview_text()
    {
        using var workspace = TestWorkspace.Create();
        workspace.WritePersona("guide", previewText: "Preview from persona.");
        using var error = new StringWriter();
        await CliApplication.RunAsync(
            ["guide", "Last", "spoken", "text."],
            TextWriter.Null,
            error,
            workspace.Root);
        using var output = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["voice", "regenerate", "guide"],
            output,
            error,
            workspace.Root);

        Assert.Equal(0, exitCode);
        Assert.Contains("preview-source: last-original-text", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Voice_regenerate_fails_when_no_preview_text_is_available()
    {
        using var workspace = TestWorkspace.Create();
        workspace.WritePersona("guide", previewText: null);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["voice", "regenerate", "guide"],
            output,
            error,
            workspace.Root);

        Assert.Equal(1, exitCode);
        Assert.Contains("No preview text is available for persona 'guide'.", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Voice_regenerate_can_use_global_preview_text()
    {
        using var workspace = TestWorkspace.Create();
        workspace.WritePersona("guide", previewText: null);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["voice", "regenerate", "guide"],
            output,
            error,
            workspace.Root,
            "Global preview.");

        Assert.Equal(0, exitCode);
        Assert.Contains("preview-source: global-preview-text", output.ToString(), StringComparison.Ordinal);
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string root)
        {
            Root = root;
            Directory.CreateDirectory(Path.Combine(root, "samples", "personas"));
        }

        public string Root { get; }

        public static TestWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "orpheus-tests", Guid.NewGuid().ToString("N"));
            return new TestWorkspace(root);
        }

        public void WritePersona(string id, string? previewText)
        {
            var previewLine = previewText is null
                ? string.Empty
                : $"""
                    "previewText": "{previewText}",
                  """;
            var json = $$"""
                {
                  "id": "{{id}}",
                  "displayName": "Test Guide",
                  "description": "A test guide.",
                  {{previewLine}}
                  "speech": {
                    "style": [ "Use plain words." ]
                  },
                  "voice": {
                    "provider": "stub",
                    "voiceId": "{{id}}-voice",
                    "style": [ "Calm." ]
                  }
                }
                """;

            File.WriteAllText(Path.Combine(Root, "samples", "personas", $"{id}.json"), json);
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
