using Orpheus.Cli;

Environment.ExitCode = await CliApplication.RunAsync(args, Console.Out, Console.Error);
