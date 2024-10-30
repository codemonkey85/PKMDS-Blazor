namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class MovesTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private Task<IEnumerable<ComboItem>> SearchMoves(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchMoves(searchString));

    private void SetPokemonMove(int moveIndex, ComboItem moveComboItem)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetMove(moveIndex, (ushort)moveComboItem.Value);

        RefreshService.Refresh();
    }

    private int GetPokemonPP(int moveIndex) =>
        Pokemon?.GetPP()[moveIndex] ?? 0;

    private void SetPokemonPP(int moveIndex, int pp)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetPP(moveIndex, pp);

        RefreshService.Refresh();
    }

    private int GetPokemonPPUps(int moveIndex) =>
        Pokemon?.GetPPUps()[moveIndex] ?? 0;

    private void SetPokemonPPUps(int moveIndex, int ppUps)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetPPUps(moveIndex, ppUps);

        RefreshService.Refresh();
    }
}
