namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MovesTab
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    private bool UseTextSearch { get; set; } = true;

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
}
