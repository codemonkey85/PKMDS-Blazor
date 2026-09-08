using PKHexSeverity = PKHeX.Core.Severity;

namespace Pkmds.Rcl;

/// <summary>
/// Shared helpers for rendering tri-state legality status (Legal / Fishy / Illegal)
/// consistently across the app. Call sites: <c>LegalityReportTab</c>,
/// <c>ShowdownImportDialog</c>, <c>PokemonStorageComponent</c>,
/// <c>PokemonSlotComponent</c>.
/// </summary>
public static class LegalityUi
{
    public static LegalityStatus GetStatus(LegalityAnalysis la)
    {
        var hasInvalid = la.Results.Any(r => r.Judgement == PKHexSeverity.Invalid)
                         || !MoveResult.AllValid(la.Info.Moves)
                         || !MoveResult.AllValid(la.Info.Relearn);

        if (hasInvalid)
        {
            return LegalityStatus.Illegal;
        }

        var hasFishy = la.Results.Any(r => r.Judgement == PKHexSeverity.Fishy);
        return hasFishy
            ? LegalityStatus.Fishy
            : LegalityStatus.Legal;
    }

    public static string GetFirstIssue(LegalityAnalysis la)
    {
        // Prefer Invalid over Fishy so the more severe issue wins when both are present.
        // CheckResult.Valid is true for Fishy judgements, so match on Judgement directly.
        foreach (var result in la.Results)
        {
            if (result.Judgement == PKHexSeverity.Invalid)
            {
                return GetDisplayMessage(la, in result);
            }
        }

        if (!MoveResult.AllValid(la.Info.Moves))
        {
            return "Invalid move detected.";
        }

        if (!MoveResult.AllValid(la.Info.Relearn))
        {
            return "Invalid relearn move detected.";
        }

        foreach (var result in la.Results)
        {
            if (result.Judgement == PKHexSeverity.Fishy)
            {
                return GetDisplayMessage(la, in result);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Humanizes a PKHeX check result while translating its technical Fishy severity
    /// into the warning terminology used by the app.
    /// </summary>
    /// <param name="la">Legality analysis that produced the result.</param>
    /// <param name="result">Check result to humanize.</param>
    /// <param name="verbose">Whether to include the check identifier in the PKHeX message.</param>
    public static string GetDisplayMessage(LegalityAnalysis la, in CheckResult result, bool verbose = false)
    {
        var ctx = LegalityLocalizationContext.Create(la);
        var message = ctx.Humanize(in result, verbose);
        if (result.Judgement != PKHexSeverity.Fishy)
        {
            return message;
        }

        var technicalLabel = ctx.Settings.Description(PKHexSeverity.Fishy);
        return message.StartsWith(technicalLabel, StringComparison.Ordinal)
            ? $"Warning{message[technicalLabel.Length..]}"
            : message;
    }

    public static Color GetStatusColor(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => Color.Success,
        LegalityStatus.Fishy => Color.Warning,
        LegalityStatus.Illegal => Color.Error,
        _ => Color.Default
    };

    public static string GetStatusIcon(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => Icons.Material.Filled.CheckCircle,
        LegalityStatus.Fishy => Icons.Material.Filled.Warning,
        LegalityStatus.Illegal => Icons.Material.Filled.Cancel,
        _ => Icons.Material.Filled.Help
    };

    public static string GetStatusLabel(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => "Legal",
        LegalityStatus.Fishy => "Legal with warnings",
        LegalityStatus.Illegal => "Illegal",
        _ => "Unknown"
    };
}
