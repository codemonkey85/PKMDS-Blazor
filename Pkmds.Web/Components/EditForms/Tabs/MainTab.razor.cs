using PKHeX.Core;

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

    private Task<IEnumerable<ComboItem>> SearchPokemonNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchPokemonNames(searchString));

    private Task<IEnumerable<ComboItem>> SearchItemNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchItemNames(searchString));

    private Task<IEnumerable<ComboItem>> SearchAbilityNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchAbilityNames(searchString));

    private bool? OnShinySet(bool shiny) => Pokemon?.SetIsShiny(shiny);

    private void OnGenderToggle()
    {
        if (Pokemon is not { PersonalInfo.IsDualGender: true, Gender: { } gender } pkm)
        {
            return;
        }

        pkm.SetGender(gender is (byte)Gender.Male ? (byte)Gender.Female : (byte)Gender.Male);
    }

    private void RevertNickname()
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.IsNicknamed = false;
        Pokemon.ClearNickname();
    }
}
