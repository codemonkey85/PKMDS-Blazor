namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MainTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    private MudSelect<byte>? FormSelect { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= Refresh;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += Refresh;

    private void Refresh()
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

        if (!nature.IsFixed())
        {
            nature = 0; // default valid
        }

        switch (Pokemon.Format)
        {
            case 3 or 4:
                Pokemon.SetPIDNature(nature);
                break;
            default:
                Pokemon.Nature = nature;
                break;
        }

        AppService.LoadPokemonStats(Pokemon);
    }

    private void OnStatNatureSet(Nature statNature)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.StatNature = statNature;
        AppService.LoadPokemonStats(Pokemon);
    }

    private Task<IEnumerable<ComboItem>> SearchPokemonNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchPokemonNames(searchString));

    private Task<IEnumerable<ComboItem>> SearchItemNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchItemNames(searchString));

    private Task<IEnumerable<ComboItem>> SearchAbilityNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchAbilityNames(searchString));

    private void OnShinySet(bool shiny) => Pokemon?.SetIsShinySafe(shiny);

    private void OnGenderToggle(Gender newGender)
    {
        if (Pokemon is not { PersonalInfo.IsDualGender: true } pkm)
        {
            return;
        }

        pkm.SetGender((byte)newGender);
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

    private void AfterFormChanged()
    {
        if (Pokemon is { Species: (ushort)Species.Indeedee })
        {
            Pokemon.SetGender(Pokemon.Form);
        }

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void SetPokemonPid(uint newPid)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.PID = newPid;

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void SetPokemonPid(string newPidHex)
    {
        if (Pokemon is null || !uint.TryParse(newPidHex, NumberStyles.HexNumber, null, out var parsedPid))
        {
            return;
        }

        Pokemon.PID = parsedPid;

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    // ReSharper disable once InconsistentNaming
    private double GetEXPToLevelUp()
    {
        if (Pokemon is not { CurrentLevel: var level and < 100, EXP: var exp, PersonalInfo.EXPGrowth: var growth })
        {
            return 0;
        }

        var table = Experience.GetTable(growth);
        var next = Experience.GetEXP(++level, table);
        return next - exp;
    }

    private void SetPokemonNickname(string newNickname)
    {
        if (Pokemon is not { Species: var species, Language: var language, Format: var format })
        {
            return;
        }

        var defaultName = SpeciesName.GetSpeciesNameGeneration(species, language, format);

        if (newNickname is not { Length: > 0 })
        {
            newNickname = defaultName;
        }

        if (newNickname is not { Length: > 0 })
        {
            return;
        }

        Pokemon.IsNicknamed = !string.Equals(newNickname, defaultName, StringComparison.Ordinal);
        Pokemon.Nickname = newNickname;
    }
}
