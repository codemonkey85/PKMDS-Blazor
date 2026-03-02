namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MainTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    private MudSelect<byte>? FormSelect { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= Refresh;

    private CheckResult? GetCheckResult(CheckIdentifier identifier)
    {
        if (Analysis is not { } la)
        {
            return null;
        }

        foreach (var r in la.Results)
        {
            if (r.Identifier == identifier && !r.Valid)
            {
                return r;
            }
        }

        return null;
    }

    private string HumanizeCheckResult(CheckResult? result)
    {
        if (result is not { } r || Analysis is not { } la)
        {
            return string.Empty;
        }

        var ctx = LegalityLocalizationContext.Create(la);
        return ctx.Humanize(in r, verbose: false);
    }

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

    private int GetAbilitySlotIndex() => Pokemon?.AbilityNumber switch
    {
        2 => 1,
        4 => 2,
        _ => 0,
    };

    private IReadOnlyList<ComboItem> GetAbilitySlotItems()
    {
        if (Pokemon is null)
        {
            return [];
        }

        var pi = Pokemon.PersonalInfo;
        var names = GameInfo.Strings.Ability;

        static string GetAbilityName(int abilityId, IReadOnlyList<string> names) =>
            (uint)abilityId < (uint)names.Count ? names[abilityId] : "None";

        var a1 = pi.AbilityCount > 0 ? pi.GetAbilityAtIndex(0) : 0;
        var a2 = pi.AbilityCount > 1 ? pi.GetAbilityAtIndex(1) : 0;
        var aH = pi.AbilityCount > 2 ? pi.GetAbilityAtIndex(2) : 0;

        return
        [
            new ComboItem($"{GetAbilityName(a1, names)} (1)", 0),
            new ComboItem($"{GetAbilityName(a2, names)} (2)", 1),
            new ComboItem($"{GetAbilityName(aH, names)} (H)", 2),
        ];
    }

    private void SetAbilitySlot(int slotIndex)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetAbilityIndex(slotIndex);
        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

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

    private async Task OpenPidEcDialog()
    {
        var parameters = new DialogParameters<PidEcDialog> { { x => x.Pokemon, Pokemon } };

        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true };

        await DialogService.ShowAsync<PidEcDialog>("PID / EC Generator", parameters, options);
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

        // For Gen I/II, verify the nickname was set correctly
        // If it becomes empty, the characters were not valid for the Pokémon's language/encoding
        if (Pokemon.Format <= 2 && string.IsNullOrEmpty(Pokemon.Nickname))
        {
            // Fallback to default name if nickname couldn't be encoded
            Pokemon.Nickname = defaultName;
            Pokemon.IsNicknamed = false;
        }
    }

    private void SetSpecies(ushort species)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.Species = species;

        // AbilityNumber 0 is invalid for every encounter. SetAbilityIndex sets
        // both the Ability ID and AbilityNumber together, which is required —
        // setting AbilityNumber alone leaves Ability at 0 (AbilityUnexpected).
        if (Pokemon.AbilityNumber is not (1 or 2 or 4))
        {
            Pokemon.SetAbilityIndex(0);
        }

        // Ensure gender is valid for the new species.
        Pokemon.Gender = Pokemon.GetSaneGender();

        // Keep the nickname in sync when it is not manually set.
        if (!Pokemon.IsNicknamed)
        {
            Pokemon.ClearNickname();
        }

        // EC = 0 causes a legality error (PIDEqualsEC when PID is also 0).
        // Generate a random EC on first species assignment so the Pokémon is
        // not flagged before the user has had a chance to fix anything.
        if (Pokemon.EncryptionConstant == 0)
        {
            Pokemon.SetRandomEC();
        }

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }
}
