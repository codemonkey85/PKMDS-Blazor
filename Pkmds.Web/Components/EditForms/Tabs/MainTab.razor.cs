namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class MainTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private MudSelect<byte>? FormSelect { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += Refresh;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= Refresh;

    public void Refresh()
    {
        FormSelect?.ForceRender(true);
        StateHasChanged();
    }

    private void OnNatureSet(Nature nature)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetNature(nature);
        AppService.LoadPokemonStats(Pokemon);
    }
}
