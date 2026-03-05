namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MovesTab
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    private bool UseTextSearch { get; set; } = true;

    private string FormatMoveMessage(MoveResult result, int index)
    {
        if (Analysis is not { } la)
        {
            return string.Empty;
        }

        var ctx = LegalityLocalizationContext.Create(la);
        return ctx.FormatMove(result, index + 1, (byte)AppState.SaveFile!.Context);
    }

    private Task<IEnumerable<ComboItem>> SearchMoves(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMoves(searchString));

    private void SetPokemonMove(int moveIndex, ComboItem? moveComboItem) =>
        SetPokemonMove(moveIndex, moveComboItem?.Value);

    private void SetPokemonMove(int moveIndex, int? newMoveId)
    {
        Pokemon?.SetMove(moveIndex, (ushort)(newMoveId ?? 0));
        if (newMoveId is not (null or 0))
        {
            return;
        }

        SetPokemonPP(moveIndex, 0);
        SetPokemonPPUps(moveIndex, 0);
    }

    private int? GetPokemonMove(int moveIndex) =>
        Pokemon?.Moves[moveIndex];

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

    private bool GetPokemonMoveIsPlus(int moveIndex) =>
        Pokemon is PA9 pa9 && pa9.GetMovePlusFlag(moveIndex);

    private void SetPokemonMoveIsPlus(int moveIndex, bool isPlus)
    {
        if (Pokemon is not PA9 pa9)
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

    private int? GetAlphaMove()
    {
        if (Pokemon is not PA8 pa8)
        {
            return null;
        }

        return pa8.AlphaMove == 0
            ? null
            : pa8.AlphaMove;
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

    private int? GetPokemonRelearnMove(int relearnIndex) =>
        Pokemon?.GetRelearnMove(relearnIndex);

    private async Task OpenRelearnFlagsDialog()
    {
        var parameters = new DialogParameters<RelearnFlagsDialog> { { x => x.Pokemon, Pokemon } };

        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true };

        await DialogService.ShowAsync<RelearnFlagsDialog>("TR Relearn Editor", parameters, options);
    }

    private async Task OpenPlusFlagsDialog()
    {
        var parameters = new DialogParameters<PlusFlagsDialog> { { x => x.Pokemon, Pokemon } };

        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true };

        await DialogService.ShowAsync<PlusFlagsDialog>("Plus Flags Editor", parameters, options);
    }

    private async Task OpenMoveShopDialog()
    {
        var parameters = new DialogParameters<MoveShopDialog> { { x => x.Pokemon, Pokemon } };

        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true };

        await DialogService.ShowAsync<MoveShopDialog>("Move Shop Editor", parameters, options);
    }
}
