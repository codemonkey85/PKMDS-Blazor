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

    internal static string MissingClosedCase(ClosedOutcome result) => result switch
    {
        ClosedSuccess => "success",
        ClosedFailure => "failure",
        ClosedTimeout => "timeout"
    };
}
#endif
