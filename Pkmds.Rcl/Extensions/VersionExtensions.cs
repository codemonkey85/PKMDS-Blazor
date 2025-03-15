namespace Pkmds.Rcl.Extensions;

public static class VersionExtensions
{
    public static string ToVersionString(this Version? version) => version == null
        ? throw new ArgumentNullException(nameof(version))
        : $"{version.Major:D4}.{version.Minor:D2}.{version.Build:D2}.{version.Revision}";
}
