namespace Pkmds.Rcl.Extensions;

/// <summary>
/// Extension methods for System.Version.
/// </summary>
public static class VersionExtensions
{
    /// <param name="version">The version to format.</param>
    extension(Version? version)
    {
        /// <summary>
        /// Converts a Version to a formatted string in the format "YYYY.MM.DD.HHMMSS".
        /// The assembly version packs day+hour into the Build component (DDHH) and
        /// minute+second into the Revision component (MMSS) to stay within the 0–65535 limit.
        /// </summary>
        /// <returns>A formatted version string (e.g., "2026.01.27.094912").</returns>
        /// <exception cref="ArgumentNullException">Thrown if version is null.</exception>
        public string ToVersionString()
        {
            if (version is null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var day = version.Build / 100;
            var hour = version.Build % 100;
            var minute = version.Revision / 100;
            var second = version.Revision % 100;
            return $"{version.Major:D4}.{version.Minor:D2}.{day:D2}.{hour:D2}{minute:D2}{second:D2}";
        }

        /// <summary>
        /// Converts a Version to a DateTime object. The components are encoded as:
        /// - Major: Year (e.g., 2026)
        /// - Minor: Month (1-12)
        /// - Build: Day and Hour packed as DDHH (e.g., 1109 = day 11, hour 09)
        /// - Revision: Minute and Second packed as MMSS (e.g., 4912 = minute 49, second 12)
        /// </summary>
        /// <returns>A DateTime object representing the version, or null if version is null.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when Build or Revision contain invalid values.</exception>
        public DateTime? ToDateTime()
        {
            if (version is null)
            {
                return null;
            }

            var day = version.Build / 100;
            var hour = version.Build % 100;
            var minute = version.Revision / 100;
            var second = version.Revision % 100;

            return day is < 1 or > 31 || hour is < 0 or > 23 || minute is < 0 or > 59 || second is < 0 or > 59
                ? throw new ArgumentOutOfRangeException(nameof(version),
                    $"Version build '{version.Build}' and revision '{version.Revision}' must be in DDHH and MMSS formats respectively.")
                : (DateTime?)DateTime.SpecifyKind(
                    new DateTime(version.Major, version.Minor, day, hour, minute, second),
                    DateTimeKind.Utc).ToLocalTime();
        }
    }
}
