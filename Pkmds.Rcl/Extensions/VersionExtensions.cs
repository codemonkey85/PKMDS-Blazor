namespace Pkmds.Rcl.Extensions;

/// <summary>
/// Extension methods for System.Version.
/// </summary>
public static class VersionExtensions
{
    /// <summary>
    /// Converts a Version to a formatted string in the format "YYYY.MM.DD.BuildNumber".
    /// </summary>
    /// <param name="version">The version to format.</param>
    /// <returns>A formatted version string (e.g., "2026.01.26.0").</returns>
    /// <exception cref="ArgumentNullException">Thrown if version is null.</exception>
    public static string ToVersionString(this Version? version) => version == null
        ? throw new ArgumentNullException(nameof(version))
        : $"{version.Major:D4}.{version.Minor:D2}.{version.Build:D2}.{version.Revision}";
}
