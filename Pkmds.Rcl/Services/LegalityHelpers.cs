using PKHexSeverity = PKHeX.Core.Severity;

namespace Pkmds.Rcl.Services;

public static class LegalityHelpers
{
    public static CheckResult? GetCheckResult(LegalityAnalysis? analysis, CheckIdentifier identifier)
    {
        if (analysis is not { } la)
        {
            return null;
        }

        // Match on Judgement (not !Valid): CheckResult.Valid is true for Fishy,
        // so filtering by Valid would hide Fishy warnings from per-field popovers.
        foreach (var r in la.Results)
        {
            if (r.Identifier == identifier && r.Judgement != PKHexSeverity.Valid)
            {
                return r;
            }
        }

        return null;
    }

    public static string Humanize(LegalityAnalysis? analysis, CheckResult? result)
    {
        if (result is not { } r || analysis is not { } la)
        {
            return string.Empty;
        }

        return LegalityUi.GetDisplayMessage(la, in r);
    }

    public static string GetIdentifierLabel(CheckIdentifier id) => id switch
    {
        CheckIdentifier.CurrentMove => "Move",
        CheckIdentifier.RelearnMove => "Relearn Move",
        CheckIdentifier.Encounter => "Encounter",
        CheckIdentifier.Shiny => "Shiny",
        CheckIdentifier.EC => "Encryption Constant",
        CheckIdentifier.PID => "PID",
        CheckIdentifier.Gender => "Gender",
        CheckIdentifier.EVs => "EVs",
        CheckIdentifier.Language => "Language",
        CheckIdentifier.Nickname => "Nickname",
        CheckIdentifier.Trainer => "Trainer",
        CheckIdentifier.IVs => "IVs",
        CheckIdentifier.Level => "Level",
        CheckIdentifier.Ball => "Ball",
        CheckIdentifier.Memory => "Memory",
        CheckIdentifier.Geography => "Geo Locations",
        CheckIdentifier.Form => "Form",
        CheckIdentifier.Egg => "Egg",
        CheckIdentifier.Misc => "Misc",
        CheckIdentifier.Fateful => "Fateful Encounter",
        CheckIdentifier.Ribbon => "Ribbon",
        CheckIdentifier.Training => "Training",
        CheckIdentifier.Ability => "Ability",
        CheckIdentifier.Evolution => "Evolution",
        CheckIdentifier.Nature => "Nature",
        CheckIdentifier.GameOrigin => "Game Origin",
        CheckIdentifier.HeldItem => "Held Item",
        CheckIdentifier.RibbonMark => "Ribbon/Mark",
        CheckIdentifier.GVs => "GVs",
        CheckIdentifier.Marking => "Marking",
        CheckIdentifier.AVs => "AVs",
        CheckIdentifier.TrashBytes => "Trash Bytes",
        CheckIdentifier.SlotType => "Slot Type",
        CheckIdentifier.Handler => "Handler",
        _ => id.ToString()
    };

    public static string GetSeverityLabel(PKHexSeverity severity) => severity switch
    {
        PKHexSeverity.Valid => "Valid",
        PKHexSeverity.Fishy => "Warning",
        _ => "Invalid"
    };
}
