namespace Pkmds.Tests;

internal static class RepoFileTestHelper
{
    private const string RepoSentinelFileName = "Pkmds.slnx";
    private static readonly string RepoRoot = FindRepoRoot();

    public static string ReadAllText(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepoRoot, .. pathSegments]));

    private static string FindRepoRoot()
    {
        var startingDirectory = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(startingDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, RepoSentinelFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException(
            $"Could not locate the repository root: {RepoSentinelFileName} was not found in or above {startingDirectory}.");
    }
}
