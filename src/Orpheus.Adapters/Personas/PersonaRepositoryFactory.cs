namespace Orpheus.Adapters.Personas;

public static class PersonaRepositoryFactory
{
    public static FilePersonaRepository CreateDefault(string? startDirectory = null)
    {
        var repositoryRoot = ResolveRepositoryRoot(startDirectory);

        return FilePersonaRepository.Load(new FilePersonaRepositoryOptions(
            Path.Combine(repositoryRoot, "samples", "personas"),
            Path.Combine(repositoryRoot, ".orpheus", "personas")));
    }

    public static string ResolveRepositoryRoot(string? startDirectory = null)
    {
        return FindRepositoryRoot(startDirectory)
            ?? FindRepositoryRoot(AppContext.BaseDirectory)
            ?? Directory.GetCurrentDirectory();
    }

    public static string ResolveRuntimeDirectory(string name, string? startDirectory = null)
    {
        return Path.Combine(ResolveRepositoryRoot(startDirectory), ".orpheus", name);
    }

    private static string? FindRepositoryRoot(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return null;
        }

        var directory = Directory.Exists(startDirectory)
            ? new DirectoryInfo(startDirectory)
            : new FileInfo(startDirectory).Directory;

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "samples", "personas")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
