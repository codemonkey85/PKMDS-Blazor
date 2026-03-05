using PKHexSeverity = PKHeX.Core.Severity;

namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class LegalityTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    // Moves are validated in la.Info.Moves / la.Info.Relearn (MoveResult[]), not in la.Results.
    // A legal Pokémon must have all of those valid in addition to all CheckResults being valid.
    private bool IsLegal => Analysis is { } la
                            && la.Results.All(r => r.Valid)
                            && MoveResult.AllValid(la.Info.Moves)
                            && MoveResult.AllValid(la.Info.Relearn);

    private bool HasRibbonIssues => Analysis is { } la &&
                                    la.Results.Any(r => r is
                                    {
                                        Valid: false, Identifier: CheckIdentifier.Ribbon or CheckIdentifier.RibbonMark
                                    });

    private bool HasMoveIssues => Analysis is { } la &&
                                  (!MoveResult.AllValid(la.Info.Moves) ||
                                   la.Results.Any(r => r is { Valid: false, Identifier: CheckIdentifier.CurrentMove }));

    private bool HasRelearnMoveIssues => Analysis is { } la &&
                                         (!MoveResult.AllValid(la.Info.Relearn) ||
                                          la.Results.Any(r => r is
                                              { Valid: false, Identifier: CheckIdentifier.RelearnMove }));

    private bool HasBallIssues => Analysis is { } la &&
                                  la.Results.Any(r => r is { Valid: false, Identifier: CheckIdentifier.Ball });

    private bool HasEncounterIssues => Analysis is { } la &&
                                       la.Results.Any(r => r is
                                           { Valid: false, Identifier: CheckIdentifier.Encounter });

    private bool HasMetLocationIssues => Analysis is { } la &&
                                         la.Results.Any(r => r is
                                         {
                                             Valid: false,
                                             Identifier: CheckIdentifier.Level or CheckIdentifier.Encounter
                                         });

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private void RemoveInvalidRibbons()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        var args = new RibbonVerifierArguments(Pokemon, la.EncounterMatch, la.Info.EvoChainsAllGens);
        RibbonApplicator.FixInvalidRibbons(in args);
        RefreshService.Refresh();
        Snackbar.Add("Invalid ribbons removed. Click Save to apply changes.", Severity.Success);
    }

    private void AddValidRibbons()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        RibbonApplicator.SetAllValidRibbons(la);
        RefreshService.Refresh();
        Snackbar.Add("All obtainable ribbons added. Click Save to apply changes.", Severity.Success);
    }

    private void SuggestMoves()
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetMoveset();

        // Update Technical Records (Gen 8+ SwSh / SV / ZA) to reflect the new moves,
        // mirroring PKMEditor's SetSuggestedMoves behaviour.
        if (Pokemon is ITechRecord tr)
        {
            tr.ClearRecordFlags();
            var freshLa = AppService.GetLegalityAnalysis(Pokemon);
            tr.SetRecordFlags(Pokemon, TechnicalRecordApplicatorOption.LegalCurrent, freshLa);
        }

        RefreshService.Refresh();
        Snackbar.Add("Moves updated with a legal move set. Click Save to apply changes.", Severity.Success);
    }

    private void SuggestRelearnMoves()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        Pokemon.SetRelearnMoves(la);
        RefreshService.Refresh();
        Snackbar.Add("Relearn moves updated. Click Save to apply changes.", Severity.Success);
    }

    private void SuggestBall()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        BallApplicator.ApplyBallLegalByColor(Pokemon, la, PersonalColorUtil.GetColor(Pokemon));
        RefreshService.Refresh();
        Snackbar.Add("Ball updated to a legal option. Click Save to apply changes.", Severity.Success);
    }

    private void SuggestMetLocation()
    {
        if (Pokemon is null)
        {
            return;
        }

        var encounter = EncounterSuggestion.GetSuggestedMetInfo(Pokemon);
        if (encounter is null)
        {
            Snackbar.Add("No met location suggestion is available for this Pokémon.", Severity.Warning);
            return;
        }

        Pokemon.MetLocation = encounter.Location;

        // If the suggested encounter is for a pre-evolution (e.g. Trophy Garden Pichu → Pikachu),
        // the Pokémon must have leveled up at least once from the encounter level to evolve.
        // Raise CurrentLevel to encounter.LevelMin + 1 before calling GetSuggestedMetLevel so
        // the brute-force loop considers a range that can include a valid MetLevel.
        if (encounter.Encounter is { } enc && enc.Species != Pokemon.Species)
        {
            var minRequired = (byte)(encounter.LevelMin + 1);
            if (Pokemon.CurrentLevel < minRequired)
            {
                Pokemon.CurrentLevel = minRequired;
                AppService.LoadPokemonStats(Pokemon);
            }
        }

        var metLevel = encounter.GetSuggestedMetLevel(Pokemon);
        Pokemon.MetLevel = metLevel;

        // A Pokémon's current level must be at least its met level.
        // For freshly-created Pokémon the level defaults to 1, so raise it now.
        if (Pokemon.CurrentLevel < metLevel)
        {
            Pokemon.CurrentLevel = metLevel;
            AppService.LoadPokemonStats(Pokemon);
        }

        RefreshService.Refresh();
        Snackbar.Add("Met location and level updated. Click Save to apply changes.", Severity.Success);
    }

    private IReadOnlyList<(MoveResult Result, int SlotNumber)> GetInvalidMoves()
    {
        if (Analysis is not { } la)
        {
            return [];
        }

        var result = new List<(MoveResult, int)>();
        var moves = la.Info.Moves;
        for (var i = 0; i < moves.Length; i++)
        {
            if (!moves[i].Valid)
            {
                result.Add((moves[i], i + 1));
            }
        }

        return result;
    }

    private IReadOnlyList<(MoveResult Result, int SlotNumber)> GetInvalidRelearnMoves()
    {
        if (Analysis is not { } la)
        {
            return [];
        }

        var result = new List<(MoveResult, int)>();
        var relearns = la.Info.Relearn;
        for (var i = 0; i < relearns.Length; i++)
        {
            if (!relearns[i].Valid)
            {
                result.Add((relearns[i], i + 1));
            }
        }

        return result;
    }

    private string GetMoveSummary(MoveResult result)
    {
        if (Analysis is not { } la)
        {
            return string.Empty;
        }

        var ctx = LegalityLocalizationContext.Create(la);
        return result.Summary(ctx);
    }

    private static Color GetSeverityColor(PKHexSeverity severity) => severity switch
    {
        PKHexSeverity.Valid => Color.Success,
        PKHexSeverity.Fishy => Color.Warning,
        _ => Color.Error
    };

    private static string GetSeverityIcon(PKHexSeverity severity) => severity switch
    {
        PKHexSeverity.Valid => Icons.Material.Filled.CheckCircle,
        PKHexSeverity.Fishy => Icons.Material.Filled.Warning,
        _ => Icons.Material.Filled.Cancel
    };

    private static string GetIdentifierLabel(CheckIdentifier id) => id switch
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

    private string HumanizeResult(CheckResult result)
    {
        if (Analysis is not { } la)
        {
            return result.Result.ToString();
        }

        var ctx = LegalityLocalizationContext.Create(la);
        return ctx.Humanize(in result);
    }
}
