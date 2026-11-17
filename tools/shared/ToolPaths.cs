namespace Pkmds.Tools;

/// <summary>Working-directory discovery shared by the file-based data tools.</summary>
internal static class ToolPaths
{
    internal static string? FindRepositoryRoot()
    {
        var directory = Environment.CurrentDirectory;
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory, "Pkmds.Rcl")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    internal static string? FindDataDirectory() => FindRepositoryRoot() is { } root
        ? Path.Combine(root, "Pkmds.Rcl", "wwwroot", "data")
        : null;
}
