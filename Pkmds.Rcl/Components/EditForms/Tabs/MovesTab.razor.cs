namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MovesTab
{
    private readonly MoveSummary?[] moveInfos = new MoveSummary?[4];
    private readonly MoveSummary?[] relearnMoveInfos = new MoveSummary?[4];
    private AbilitySummary? abilityInfo;
    private MoveSummary? alphaMoveInfo;

    // Cached move summaries — reloaded when the Pokemon reference changes.
    private PKM? lastPokemon;

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(Pokemon, lastPokemon))
        {
            return;
        }

        lastPokemon = Pokemon;
        await LoadMoveInfosAsync();
    }

    private async Task LoadMoveInfosAsync()
    {
        if (Pokemon is null || AppState.SaveFile is null)
        {
            return;
        }

        var version = AppState.SaveFile.Version;

        abilityInfo = Pokemon.Ability != 0
            ? await DescriptionService.GetAbilityInfoAsync(Pokemon.Ability, version)
            : null;

        for (var i = 0; i < Pokemon.Moves.Length && i < moveInfos.Length; i++)
        {
            var moveId = Pokemon.Moves[i];
            moveInfos[i] = moveId != 0
                ? await DescriptionService.GetMoveInfoAsync(moveId, version)
                : null;
        }

        if (Pokemon.HasRelearnMoves())
        {
            for (var i = 0; i < relearnMoveInfos.Length; i++)
            {
                var moveId = Pokemon.GetRelearnMove(i);
                relearnMoveInfos[i] = moveId != 0
                    ? await DescriptionService.GetMoveInfoAsync(moveId, version)
                    : null;
            }
        }

        if (Pokemon is PA8 pa8Alpha)
        {
            var alphaMoveId = pa8Alpha.AlphaMove;
            alphaMoveInfo = alphaMoveId != 0
                ? await DescriptionService.GetMoveInfoAsync(alphaMoveId, version)
                : null;
        }
    }

    private string FormatMoveMessage(MoveResult result, int index)
    {
        if (Analysis is not { } la)
        {
            return string.Empty;
        }

        var ctx = LegalityLocalizationContext.Create(la);
        return ctx.FormatMove(result, index + 1, AppState.SaveFile!.Context);
    }

    private void SetPokemonMove(int moveIndex, ComboItem? moveComboItem) =>
        SetPokemonMove(moveIndex, moveComboItem?.Value);

    private void SetPokemonMove(int moveIndex, int? newMoveId)
    {
        Pokemon?.SetMove(moveIndex, (ushort)(newMoveId ?? 0));
        if (newMoveId is not (null or 0))
        {
            _ = RefreshMoveInfoAsync(moveIndex, newMoveId.Value);
            return;
        }

        moveInfos[moveIndex] = null;
        SetPokemonPP(moveIndex, 0);
        SetPokemonPPUps(moveIndex, 0);
    }

    private async Task RefreshMoveInfoAsync(int moveIndex, int moveId)
    {
        if (AppState.SaveFile is not { } sav)
        {
            return;
        }

        moveInfos[moveIndex] = await DescriptionService.GetMoveInfoAsync(moveId, sav.Version);
        StateHasChanged();
    }

    // Human-readable labels for PokeAPI move flag identifiers.

    // ReSharper disable once InconsistentNaming
    private int GetPokemonPP(int moveIndex) =>
        Pokemon?.GetPP()[moveIndex] ?? 0;

    // ReSharper disable once InconsistentNaming
    private void SetPokemonPP(int moveIndex, int pp) =>
        Pokemon?.SetPP(moveIndex, pp);

    // ReSharper disable once InconsistentNaming
    private int GetPokemonPPUps(int moveIndex) =>
        Pokemon?.GetPPUps()[moveIndex] ?? 0;

    // ReSharper disable once InconsistentNaming
    private void SetPokemonPPUps(int moveIndex, int ppUps) =>
        Pokemon?.SetPPUps(moveIndex, ppUps);

    private bool GetPokemonMoveIsPlus(int moveIndex)
    {
        if (moveIndex < 0 || Pokemon is not PA9 pa9)
        {
            return false;
        }

        return pa9.GetMovePlusFlag(moveIndex);
    }

    private void SetPokemonMoveIsPlus(int moveIndex, bool isPlus)
    {
        if (moveIndex < 0 || Pokemon is not PA9 pa9)
        {
            return;
        }

        pa9.SetMovePlusFlag(moveIndex, isPlus);
    }

    private int GetMasteredRecordIndex(ushort moveId)
    {
        if (Pokemon is not IMoveShop8 moveShop)
        {
            return -1;
        }

        var permit = moveShop.Permit;
        var indexes = permit.RecordPermitIndexes;

        for (var i = 0; i < indexes.Length; i++)
        {
            if (indexes[i] == moveId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool GetPokemonMoveIsMastered(int recordIndex)
    {
        if (recordIndex < 0 || Pokemon is not IMoveShop8Mastery mastery)
        {
            return false;
        }

        return mastery.GetMasteredRecordFlag(recordIndex);
    }

    private void SetPokemonMoveIsMastered(int recordIndex, bool isMastered)
    {
        if (recordIndex < 0 || Pokemon is not IMoveShop8Mastery mastery)
        {
            return;
        }

        mastery.SetMasteredRecordFlag(recordIndex, isMastered);
    }

    private void SetAlphaMove(ComboItem? moveComboItem) =>
        SetAlphaMove(moveComboItem?.Value);

    private void SetAlphaMove(int? newMoveId)
    {
        if (Pokemon is not PA8 pa8)
        {
            return;
        }

        pa8.AlphaMove = (ushort)(newMoveId ?? 0);
    }

    private void SetPokemonRelearnMove(int relearnIndex, ComboItem? moveComboItem) =>
        SetPokemonRelearnMove(relearnIndex, moveComboItem?.Value);

    private void SetPokemonRelearnMove(int relearnIndex, int? newMoveId) =>
        Pokemon?.SetRelearnMove(relearnIndex, (ushort)(newMoveId ?? 0));

    private async Task OpenRelearnFlagsDialog()
    {
        var parameters = new DialogParameters<RelearnFlagsDialog> { { x => x.Pokemon, Pokemon } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<RelearnFlagsDialog>("TR Relearn Editor", parameters, options);
    }

    private async Task OpenPlusFlagsDialog()
    {
        var parameters = new DialogParameters<PlusFlagsDialog> { { x => x.Pokemon, Pokemon } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<PlusFlagsDialog>("Plus Flags Editor", parameters, options);
    }

    private async Task OpenMoveShopDialog()
    {
        var parameters = new DialogParameters<MoveShopDialog> { { x => x.Pokemon, Pokemon } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<MoveShopDialog>("Move Shop Editor", parameters, options);
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

    private void ApplyFixOutcome(FixOutcome outcome)
    {
        Snackbar.Add(outcome.Message, outcome.Severity);
        if (outcome.Changed)
        {
            RefreshService.Refresh();
        }
    }
}
