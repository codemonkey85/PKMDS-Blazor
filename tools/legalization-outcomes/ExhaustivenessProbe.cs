#if VERIFY_EXHAUSTIVENESS
namespace Pkmds.Tools.LegalizationOutcomes;

// Intentional compiler errors, enabled only for the negative build probe.
// A null arm must not hide a newly added case as a discard arm would.
internal static class ExhaustivenessProbe
{
    internal static string MissingUnionCase(UnionOutcome result) => result switch
    {
        Succeeded => "success",
        Failed => "failure",
        TimedOut => "timeout",
        null => "uninitialized"
    };

    internal static string MissingClosedCase(LegalizationOutcome result) => result switch
    {
        LegalizationSuccess => "success",
        LegalizationFailure => "failure",
        LegalizationTimeout => "timeout"
    };
}
#endif
