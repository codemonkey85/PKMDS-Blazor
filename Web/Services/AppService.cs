namespace Pkmds.Web.Services;

public record AppService(IAppState AppState, IRefreshService RefreshService) : IAppService
{
    private const string EnglishLang = "en";
    private const string defaultPkmFileName = "pkm.bin";

    private PKM? editFormPokemon;
    private bool isDrawerOpen;

    public string[] NatureStatShortNames => ["Atk", "Def", "Spe", "SpA", "SpD"];

    public PKM? EditFormPokemon
    {
        get => editFormPokemon;
        set
        {
            editFormPokemon = value?.Clone();
            LoadPokemonStats(editFormPokemon);
        }
    }

    public bool IsDrawerOpen
    {
        get => isDrawerOpen;
        set
        {
            isDrawerOpen = value;
            RefreshService.Refresh();
        }
    }

    public void ToggleDrawer() => IsDrawerOpen = !IsDrawerOpen;

    public void ClearSelection()
    {
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        AppState.SelectedPartySlotNumber = null;
        EditFormPokemon = null;
        RefreshService.Refresh();
    }

    public IEnumerable<ComboItem> SearchPokemonNames(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Species
            .Where(species => species.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(species => species.Text);

    public ComboItem GetSpeciesComboItem(ushort speciesId) => GameInfo.FilteredSources.Species
        .FirstOrDefault(species => species.Value == speciesId) ?? default!;

    public IEnumerable<ComboItem> SearchItemNames(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Items
            .Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Text);

    public ComboItem GetItemComboItem(int itemId) => GameInfo.FilteredSources.Items
        .FirstOrDefault(item => item.Value == itemId) ?? default!;

    public IEnumerable<ComboItem> SearchAbilityNames(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Abilities
            .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ability => ability.Text);

    public ComboItem GetAbilityComboItem(int abilityId) => GameInfo.FilteredSources.Abilities
        .FirstOrDefault(ability => ability.Value == abilityId) ?? default!;

    public string GetStatModifierString(Nature nature)
    {
        var (up, down) = NatureAmp.GetNatureModification(nature);
        return up == down ? string.Empty : $"({NatureStatShortNames[up]} ↑, {NatureStatShortNames[down]} ↓)";
    }

    public void LoadPokemonStats(PKM? pokemon)
    {
        if (AppState.SaveFile is null || pokemon is null)
        {
            return;
        }

        var pt = AppState.SaveFile.Personal;
        var pi = pt.GetFormEntry(pokemon.Species, pokemon.Form);
        Span<ushort> stats = stackalloc ushort[6];
        pokemon.LoadStats(pi, stats);
        pokemon.SetStats(stats);
    }

    public IEnumerable<ComboItem> SearchMetLocations(string searchString, bool isEggLocation = false) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.GetLocationList(AppState.SaveFile.Version, AppState.SaveFile.Context, isEggLocation)
            .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ability => ability.Text);

    public ComboItem GetMetLocationComboItem(int metLocationId, bool isEggLocation = false) => AppState.SaveFile is null
        ? default!
        : GameInfo.GetLocationList(AppState.SaveFile.Version, AppState.SaveFile.Context, isEggLocation)
            .FirstOrDefault(metLocation => metLocation.Value == metLocationId) ?? default!;

    public IEnumerable<ComboItem> SearchMoves(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Moves
            .Where(move => move.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(move => move.Text);

    public ComboItem GetMoveComboItem(int moveId) => GameInfo.FilteredSources.Moves
        .FirstOrDefault(metLocation => metLocation.Value == moveId) ?? default!;

    public void SavePokemon(PKM? pokemon)
    {
        if (AppState.SaveFile is null || pokemon is null)
        {
            return;
        }

        if (AppState.SelectedPartySlotNumber is not null)
        {
            AppState.SaveFile.SetPartySlotAtIndex(pokemon, AppState.SelectedPartySlotNumber.Value);
            RefreshService.RefreshPartyState();
        }
        else if (AppState.SelectedBoxNumber is not null && AppState.SelectedBoxSlotNumber is not null)
        {
            AppState.SaveFile.SetBoxSlotAtIndex(pokemon, AppState.SelectedBoxNumber.Value, AppState.SelectedBoxSlotNumber.Value);
            RefreshService.RefreshBoxState();
        }
    }

    public string GetCleanFileName(PKM pkm) => pkm.Context switch
    {
        EntityContext.SplitInvalid or EntityContext.MaxInvalid => defaultPkmFileName,
        EntityContext.Gen1 or EntityContext.Gen2 => pkm switch
        {
            PK1 pk1 => $"{GameInfo.GetStrings(EnglishLang).Species[pk1.Species]}_{pk1.DV16}.{pk1.Extension}",
            PK2 pk2 => $"{GameInfo.GetStrings(EnglishLang).Species[pk2.Species]}_{pk2.DV16}.{pk2.Extension}",
            _ => defaultPkmFileName,
        },
        _ => $"{GameInfo.GetStrings(EnglishLang).Species[pkm.Species]}_{pkm.PID:X}.{pkm.Extension}",
    };

    public void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber)
    {
        AppState.SelectedPartySlotNumber = null;

        AppState.SelectedBoxNumber = boxNumber;
        AppState.SelectedBoxSlotNumber = slotNumber;
        EditFormPokemon = pkm;

        RefreshService.Refresh();
    }

    public void SetSelectedPartyPokemon(PKM? pkm, int slotNumber)
    {
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        AppState.SelectedPartySlotNumber = slotNumber;
        EditFormPokemon = pkm;

        RefreshService.Refresh();
    }

    public void DeletePokemon()
    {
    }

    public string ExportPokemonAsShowdown(PKM? pkm) => pkm is null
        ? string.Empty
        : ShowdownParsing.GetShowdownText(pkm);

    public string ExportPartyAsShowdown()
    {
        if (AppState.SaveFile is not { HasParty: true, PartyCount: var partyCount } saveFile)
        {
            return string.Empty;
        }

        var sbShowdown = new StringBuilder();

        for (var slot = 0; slot < partyCount; slot++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(slot);
            if (pkm is null)
            {
                continue;
            }

            sbShowdown.AppendLine(ShowdownParsing.GetShowdownText(pkm));
            sbShowdown.AppendLine();
        }

        return sbShowdown.ToString().Trim();
    }

    public string GetIdFormatString(bool isSid = false)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return string.Empty;
        }

        var format = saveFile?.GetTrainerIDFormat();
        return (format, isSid) switch
        {
            (TrainerIDFormat.SixteenBit, false) => TrainerIDExtensions.TID16,
            (TrainerIDFormat.SixteenBit, true) => TrainerIDExtensions.SID16,
            (TrainerIDFormat.SixDigit, false) => TrainerIDExtensions.TID7,
            (TrainerIDFormat.SixDigit, true) => TrainerIDExtensions.SID7,
            _ => "D"
        };
    }
}
