namespace Pkmds.Web.Services;

public record AppService(IAppState AppState, IRefreshService RefreshService) : IAppService
{
    private const string EnglishLang = "en";
    private const string DefaultPkmFileName = "pkm.bin";

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

    public string GetPokemonSpeciesName(ushort speciesId) => GetSpeciesComboItem(speciesId)?.Text ?? string.Empty;

    public IEnumerable<ComboItem> SearchPokemonNames(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Species
            .DistinctBy(species => species.Value)
            .Where(species => species.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(species => species.Text);

    public ComboItem GetSpeciesComboItem(ushort speciesId) => GameInfo.FilteredSources.Species
        .DistinctBy(species => species.Value)
        .FirstOrDefault(species => species.Value == speciesId) ?? default!;

    public IEnumerable<ComboItem> SearchItemNames(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Items
            .DistinctBy(item => item.Value)
            .Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Text);

    public ComboItem GetItemComboItem(int itemId) => GameInfo.FilteredSources.Items
        .DistinctBy(item => item.Value)
        .FirstOrDefault(item => item.Value == itemId) ?? default!;

    public ComboItem GetAbilityComboItem(int abilityId) => GameInfo.FilteredSources.Abilities
        .DistinctBy(ability => ability.Value)
        .FirstOrDefault(ability => ability.Value == abilityId) ?? default!;

    public IEnumerable<ComboItem> SearchAbilityNames(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
    ? []
    : GameInfo.FilteredSources.Abilities
        .DistinctBy(ability => ability.Value)
        .Where(ability => ability.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
        .OrderBy(ability => ability.Text);

    public string GetStatModifierString(Nature nature)
    {
        var (up, down) = NatureAmp.GetNatureModification(nature);
        return up == down ? "(neutral)" : $"({NatureStatShortNames[up]} ↑, {NatureStatShortNames[down]} ↓)";
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
        : GameInfo.GetLocationList(AppState.SaveFile.Version.GetSingleVersion(), AppState.SaveFile.Context, isEggLocation)
            .DistinctBy(l => l.Value)
            .Where(metLocation => metLocation.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(metLocation => metLocation.Text);

    public ComboItem GetMetLocationComboItem(ushort metLocationId, bool isEggLocation = false) => AppState.SaveFile is null
        ? default!
        : GameInfo.GetLocationList(AppState.SaveFile.Version.GetSingleVersion(), AppState.SaveFile.Context, isEggLocation)
            .DistinctBy(l => l.Value)
            .FirstOrDefault(metLocation => metLocation.Value == metLocationId) ?? default!;

    public IEnumerable<ComboItem> SearchMoves(string searchString) => AppState.SaveFile is null || searchString is not { Length: > 0 }
        ? []
        : GameInfo.FilteredSources.Moves
            .DistinctBy(move => move.Value)
            .Where(move => move.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(move => move.Text);

    public ComboItem GetMoveComboItem(int moveId) => GameInfo.FilteredSources.Moves
        .DistinctBy(move => move.Value)
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

            if (AppState.SaveFile is SAV7b)
            {
                RefreshService.RefreshBoxAndPartyState();
            }
            else
            {
                RefreshService.RefreshPartyState();
            }
        }
        else if (AppState.SelectedBoxNumber is not null && AppState.SelectedBoxSlotNumber is not null)
        {
            AppState.SaveFile.SetBoxSlotAtIndex(pokemon, AppState.SelectedBoxNumber.Value, AppState.SelectedBoxSlotNumber.Value);
            RefreshService.RefreshBoxState();
        }
        else if (AppState.SelectedBoxNumber is null && AppState.SelectedBoxSlotNumber is not null && AppState.SaveFile is SAV7b)
        {
            AppState.SaveFile.SetBoxSlotAtIndex(pokemon, AppState.SelectedBoxSlotNumber.Value);
            RefreshService.RefreshBoxAndPartyState();
        }
    }

    public string GetCleanFileName(PKM pkm) => pkm.Context switch
    {
        EntityContext.SplitInvalid or EntityContext.MaxInvalid => DefaultPkmFileName,
        EntityContext.Gen1 or EntityContext.Gen2 => pkm switch
        {
            PK1 pk1 => $"{GameInfo.GetStrings(EnglishLang).Species[pk1.Species]}_{pk1.DV16}.{pk1.Extension}",
            PK2 pk2 => $"{GameInfo.GetStrings(EnglishLang).Species[pk2.Species]}_{pk2.DV16}.{pk2.Extension}",
            _ => DefaultPkmFileName,
        },
        _ => $"{GameInfo.GetStrings(EnglishLang).Species[pkm.Species]}_{pkm.PID:X}.{pkm.Extension}",
    };

    public void SetSelectedLetsGoPokemon(PKM? pkm, int slotNumber)
    {
        AppState.SelectedPartySlotNumber = null;

        AppState.SelectedBoxSlotNumber = slotNumber;
        EditFormPokemon = pkm;

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    public void SetSelectedBoxPokemon(PKM? pkm, int boxNumber, int slotNumber)
    {
        AppState.SelectedPartySlotNumber = null;

        AppState.SelectedBoxNumber = boxNumber;
        AppState.SelectedBoxSlotNumber = slotNumber;
        EditFormPokemon = pkm;

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    public void SetSelectedPartyPokemon(PKM? pkm, int slotNumber)
    {
        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        AppState.SelectedPartySlotNumber = slotNumber;
        EditFormPokemon = pkm;

        HandleNullOrEmptyPokemon();
        RefreshService.Refresh();
    }

    private void HandleNullOrEmptyPokemon()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        EditFormPokemon ??= saveFile.BlankPKM;

        if (EditFormPokemon is { Species: (ushort)Species.None })
        {
            EditFormPokemon.Version = saveFile.Version.GetSingleVersion();
        }
    }

    public void DeletePokemon(int partySlotNumber)
    {
        if (AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        saveFile.DeletePartySlot(partySlotNumber);

        AppState.SelectedPartySlotNumber = null;

        RefreshService.RefreshPartyState();
    }

    public void DeletePokemon(int boxNumber, int boxSlotNumber)
    {
        if (AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        saveFile.SetBoxSlotAtIndex(saveFile.BlankPKM, boxNumber, boxSlotNumber);

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
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

        var format = saveFile.GetTrainerIDFormat();
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
