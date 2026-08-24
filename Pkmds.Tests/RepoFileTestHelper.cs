namespace Pkmds.Tests;

internal static class RepoFileTestHelper
{
    private static readonly string RepoRoot = FindRepoRoot();

    public static string ReadAllText(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepoRoot, .. pathSegments]));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pkmds.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
