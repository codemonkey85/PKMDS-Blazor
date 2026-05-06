using PKHexSeverity = PKHeX.Core.Severity;

namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class LegalityTab : IDisposable
{
    private string? legalizationProgress;

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    /// <summary>
    /// Callback invoked when legalization replaces the Pokémon with a new legal clone.
    /// The parent (PokemonEditForm) should update its EditFormPokemon and recompute analysis.
    /// </summary>
    [Parameter]
    public EventCallback<PKM> OnPokemonLegalized { get; set; }

    private bool IsLegalizing { get; set; }

    // Moves are validated in la.Info.Moves / la.Info.Relearn (MoveResult[]), not in la.Results.
    // A legal Pokémon must have all of those valid in addition to all CheckResults being valid.
    private bool IsLegal => Analysis is { } la
                            && la.Results.All(r => r.Valid)
                            && MoveResult.AllValid(la.Info.Moves)
                            && MoveResult.AllValid(la.Info.Relearn);

    // Fishy = passes LegalityAnalysis.Valid but at least one CheckResult has a Fishy
    // judgement. CheckResult.Valid is true for Fishy, so plain IsLegal collapses it
    // into Legal — distinguish it here so the alert can show a warning severity
    // matching what the Legality Report tab reports.
    private bool IsFishy => IsLegal
                            && Analysis is { } la
                            && la.Results.Any(r => r.Judgement == PKHexSeverity.Fishy);

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

    private bool HasTrashByteIssues => Analysis is { } la &&
                                       la.Results.Any(r => !r.Valid && IsTrashByteResultCode(r.Result));

    // PalPark trash bytes (Gen 3 → Gen 4 transfer) are not deterministically recoverable.
    private bool HasPalParkTrashByteIssues => HasTrashByteIssues &&
                                              Pokemon is { Format: 4 } &&
                                              Analysis is { EncounterMatch.Generation: 3 };

    private bool CanAutoFixTrashBytes => HasTrashByteIssues &&
                                         Pokemon is not null &&
                                         Analysis is { } la &&
                                         LegalityFixService.CanAutoFixTrashBytes(Pokemon, la);

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private void ShowSuccessSnackbar(PKM legalizedPokemon, LegalizationChanges changes)
    {
        if (changes.IsEmpty)
        {
            Snackbar.Add("Legalized successfully. Click Save to apply changes.", Severity.Success);
            return;
        }

        var plural = changes.Count == 1 ? string.Empty : "s";
        var message = $"Legalized successfully — {changes.Count} change{plural}. Click Save to apply.";
        Snackbar.Add(message, Severity.Success, config =>
        {
            config.Action = "View changes";
            config.ActionColor = Color.Inherit;
            config.OnClick = _ => ShowChangesDialog(legalizedPokemon, changes);
        });
    }

    private async Task ShowChangesDialog(PKM pokemon, LegalizationChanges changes)
    {
        var label = AppService.GetPokemonSpeciesName(pokemon.Species) ?? string.Empty;
        var parameters = new DialogParameters<LegalizationChangesDialog>
        {
            { x => x.Changes, changes },
            { x => x.PokemonLabel, label }
        };

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<LegalizationChangesDialog>(
            "Legalization Changes", parameters, options);
    }

    private async Task LegalizeAsync()
    {
        if (Pokemon is null || AppState.SaveFile is not { } sav)
        {
            return;
        }

        IsLegalizing = true;
        legalizationProgress = null;
        StateHasChanged();

        try
        {
            var progress = new Progress<string>(msg =>
            {
                legalizationProgress = msg;
                StateHasChanged();
            });

            var result = await LegalizationService.LegalizeAsync(Pokemon, sav, progress);

            switch (result.Status)
            {
                case LegalizationStatus.Success:
                    await OnPokemonLegalized.InvokeAsync(result.Pokemon);
                    ShowSuccessSnackbar(result.Pokemon, result.Changes);
                    break;

                case LegalizationStatus.SuccessHaX:
                    // HaX retry replaced the editor PKM with a populated-but-illegal result.
                    // Still invoke OnPokemonLegalized so the editor reflects the change; the
                    // legality alert will repaint as Invalid.
                    await OnPokemonLegalized.InvokeAsync(result.Pokemon);
                    Snackbar.Add(
                        result.FailureReason ?? "Imported via HaX retry — illegal but populated.",
                        Severity.Warning);
                    break;

                case LegalizationStatus.Timeout:
                    Snackbar.Add(result.FailureReason ?? "Legalization timed out.", Severity.Warning);
                    break;

                case LegalizationStatus.Failed:
                    Snackbar.Add(result.FailureReason ?? "Could not find a legal encounter.", Severity.Warning);
                    break;
            }
        }
        finally
        {
            IsLegalizing = false;
            legalizationProgress = null;
            StateHasChanged();
        }
    }

    private static bool IsTrashByteResultCode(LegalityCheckResultCode code) => code is
        LegalityCheckResultCode.TrashBytesExpected or
        LegalityCheckResultCode.TrashBytesMismatchInitial or
        LegalityCheckResultCode.TrashBytesMissingTerminatorFinal or
        LegalityCheckResultCode.TrashBytesShouldBeEmpty;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private void RemoveInvalidRibbons()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.RemoveInvalidRibbons(Pokemon, la));
    }

    private void AddValidRibbons()
    {
        if (Analysis is not { } la)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.AddValidRibbons(la));
    }

    private void SuggestMoves()
    {
        if (Pokemon is null)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.SuggestMoves(Pokemon));
    }

    private void SuggestRelearnMoves()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.SuggestRelearnMoves(Pokemon, la));
    }

    private void SuggestBall()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.SuggestBall(Pokemon, la));
    }

    private void SuggestMetLocation()
    {
        if (Pokemon is null)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.SuggestMetLocation(Pokemon));
    }

    private void FixTrashBytes()
    {
        if (Pokemon is null || Analysis is not { } la)
        {
            return;
        }

        ApplyFixOutcome(LegalityFixService.FixTrashBytes(Pokemon, la));
    }

    private void ApplyFixOutcome(FixOutcome outcome)
    {
        Snackbar.Add(outcome.Message, outcome.Severity);
        if (outcome.Changed)
        {
            RefreshService.Refresh();
        }
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
