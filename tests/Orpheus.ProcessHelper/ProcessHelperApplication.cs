namespace Orpheus.ProcessHelper;

public static class ProcessHelperMarker;

public static class ProcessHelperApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        var options = ParseArgs(args);

        if (options.ExitCode != 0)
        {
            Console.Error.WriteLine("Configured helper failure.");
            return options.ExitCode;
        }

        if (options.DelayMilliseconds > 0)
        {
            await Task.Delay(options.DelayMilliseconds);
        }

        if (options.SkipOutput)
        {
            return 0;
        }

        if (string.IsNullOrWhiteSpace(options.TextFile) || string.IsNullOrWhiteSpace(options.Output))
        {
            Console.Error.WriteLine("--text-file and --output are required.");
            return 2;
        }

        var text = await File.ReadAllTextAsync(options.TextFile);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.Output))!);
        if (options.WriteEmpty)
        {
            await File.WriteAllBytesAsync(options.Output, []);
            return 0;
        }

        await WriteWaveFileAsync(options.Output, text);
        return 0;
    }

    private static HelperOptions ParseArgs(string[] args)
    {
        var options = new HelperOptions();

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--text-file":
                    options.TextFile = args[++index];
                    break;
                case "--output":
                    options.Output = args[++index];
                    break;
                case "--reference-audio":
                    options.ReferenceAudio = args[++index];
                    break;
                case "--exit-code":
                    options.ExitCode = int.Parse(args[++index]);
                    break;
                case "--delay-ms":
                    options.DelayMilliseconds = int.Parse(args[++index]);
                    break;
                case "--skip-output":
                    options.SkipOutput = true;
                    break;
                case "--write-empty":
                    options.WriteEmpty = true;
                    break;
            }
        }

        return options;
    }

    private static async Task WriteWaveFileAsync(string outputPath, string text)
    {
        const int sampleRate = 8000;
        const short channels = 1;
        const short bitsPerSample = 16;
        const short blockAlign = channels * bitsPerSample / 8;
        const int byteRate = sampleRate * blockAlign;
        var durationSeconds = Math.Clamp(0.2 + text.Length * 0.004, 0.2, 1.0);
        var sampleCount = (int)(sampleRate * durationSeconds);
        var dataSize = sampleCount * blockAlign;

        await using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        for (var index = 0; index < sampleCount; index++)
        {
            writer.Write((short)0);
        }
    }

    private sealed class HelperOptions
    {
        public string? TextFile { get; set; }

        public string? Output { get; set; }

        public string? ReferenceAudio { get; set; }

        public int ExitCode { get; set; }

        public int DelayMilliseconds { get; set; }

        public bool SkipOutput { get; set; }

        public bool WriteEmpty { get; set; }
    }
}
