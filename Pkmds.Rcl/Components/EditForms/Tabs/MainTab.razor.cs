namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class MainTab : IDisposable
{
    // Used by the small picker dialogs (Pumpkaboo size, Minior color, Flower color,
    // Spinda pattern) that look fine on mobile without going full-screen.
    private static readonly DialogOptions AppearanceDialogOptions = new() { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true };
    private AbilitySummary? abilityInfo;
    private ItemSummary? heldItemInfo;

    private PKM? lastPokemon;

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    private bool IsAlcremie => Pokemon?.Species == (ushort)Species.Alcremie;

    private bool IsVivillon =>
        Pokemon?.Species is (ushort)Species.Scatterbug or (ushort)Species.Spewpa or (ushort)Species.Vivillon;

    private bool IsFurfrou => Pokemon?.Species == (ushort)Species.Furfrou;

    private bool IsPumpkabooOrGourgeist =>
        Pokemon?.Species is (ushort)Species.Pumpkaboo or (ushort)Species.Gourgeist;

    private bool IsMinior => Pokemon?.Species == (ushort)Species.Minior;

    private bool IsFlabebebFamily =>
        Pokemon?.Species is (ushort)Species.Flabébé or (ushort)Species.Floette or (ushort)Species.Florges;

    private bool IsSpinda => Pokemon?.Species == (ushort)Species.Spinda;

    private bool CanEvolve =>
        Pokemon is { IsEgg: false } &&
        AppService.GetDirectEvolutions(Pokemon).Count > 0;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= Refresh;

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(Pokemon, lastPokemon))
        {
            return;
        }

        lastPokemon = Pokemon;
        await LoadDescriptionsAsync();
    }

    private async Task LoadDescriptionsAsync()
    {
        if (Pokemon is null || AppState.SaveFile is not { } sav)
        {
            return;
        }

        var version = sav.Version;

        var itemName = Pokemon.HeldItem != 0
            ? AppService.GetItemComboItem(Pokemon.HeldItem).Text
            : null;
        heldItemInfo = itemName is not null
            ? await DescriptionService.GetItemInfoAsync(itemName, version)
            : null;

        abilityInfo = Pokemon.Ability != 0
            ? await DescriptionService.GetAbilityInfoAsync(Pokemon.Ability, version)
            : null;
    }

    private void SetHeldItem(ComboItem? item)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.HeldItem = item?.Value ?? 0;
        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
        if (Pokemon.HeldItem != 0)
        {
            _ = RefreshHeldItemInfoAsync(item?.Text);
        }
        else
        {
            heldItemInfo = null;
        }
    }

    private async Task RefreshHeldItemInfoAsync(string? itemName)
    {
        if (AppState.SaveFile is not { } sav || itemName is null)
        {
            return;
        }

        heldItemInfo = await DescriptionService.GetItemInfoAsync(itemName, sav.Version);
        StateHasChanged();
    }

    private async Task RefreshAbilityInfoAsync()
    {
        if (Pokemon is null || AppState.SaveFile is not { } sav)
        {
            return;
        }

        abilityInfo = Pokemon.Ability != 0
            ? await DescriptionService.GetAbilityInfoAsync(Pokemon.Ability, sav.Version)
            : null;
        StateHasChanged();
    }

    /// <summary>
    /// Returns the sprite filename for the form-dropdown preview image.
    /// For Scatterbug and Spewpa, substitutes Vivillon's species so the preview
    /// shows the actual wing pattern rather than an identical caterpillar silhouette.
    /// </summary>
    private string GetFormPreviewSprite() => Pokemon is null
        ? ImageHelper.PokemonFallbackImageFileName
        : IsVivillon
            ? ImageHelper.GetPokemonSpriteFilenameForForm((ushort)Species.Vivillon, Pokemon.Context, Pokemon.Form)
            : ImageHelper.GetPokemonSpriteFilename(Pokemon);

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += Refresh;

    private void Refresh() => StateHasChanged();

    private static IEnumerable<int> LanguageItems =>
        GameInfo.FilteredSources.Languages.DistinctBy(l => l.Value).Select(l => l.Value);

    private static string GetLanguageText(int value) =>
        GameInfo.FilteredSources.Languages.FirstOrDefault(l => l.Value == value)?.Text ?? value.ToString();

    private void OnNatureSet(Nature nature)
    {
        if (Pokemon is null)
        {
            return;
        }

        if (!nature.IsFixed)
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

        Pokemon.StatAlignment = statNature;
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
        _ => 0
    };

    private IReadOnlyList<ComboItem> GetAbilitySlotItems()
    {
        if (Pokemon is null)
        {
            return [];
        }

        var pi = Pokemon.PersonalInfo;
        var names = GameInfo.Strings.Ability;

        List<ComboItem> items = [];
        for (var i = 0; i < pi.AbilityCount; i++)
        {
            var abilityId = pi.GetAbilityAtIndex(i);
            var suffix = i switch { 0 => "1", 1 => "2", _ => "H" };
            items.Add(new ComboItem($"{GetAbilityName(abilityId, names)} ({suffix})", i));
        }

        return items;

        static string GetAbilityName(int abilityId, IReadOnlyList<string> names) =>
            abilityId == 0 || (uint)abilityId >= (uint)names.Count
                ? "None"
                : names[abilityId];
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
        _ = RefreshAbilityInfoAsync();
    }

    // ── HaX DEV_Ability helpers (any ability ID, Gen 4+) ─────────────────────

    private ComboItem GetDevAbilityComboItem()
    {
        if (Pokemon is null)
        {
            return new ComboItem("None", 0);
        }

        var names = GameInfo.Strings.Ability;
        var id = Pokemon.Ability;
        var name = id == 0
            ? "None"
            : (uint)id >= (uint)names.Count
                ? $"(Ability #{id})"
                : names[id];
        return new ComboItem(name, id);
    }

    private static Task<IEnumerable<ComboItem>> SearchAllAbilities(string searchString, CancellationToken _)
    {
        var names = GameInfo.Strings.Ability;
        var source = names
            .Select((name, i) => new ComboItem(name, i))
            .Where(item => !string.IsNullOrEmpty(item.Text))
            .OrderBy(item => item.Text);

        IEnumerable<ComboItem> results = string.IsNullOrWhiteSpace(searchString)
            ? source.Take(30)
            : source.Where(item => item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(results);
    }

    private void SetDevAbility(ComboItem? item)
    {
        if (Pokemon is null || item is null || AppState?.IsHaXEnabled is not true)
        {
            return;
        }

        Pokemon.Ability = (ushort)item.Value;
        RefreshService.Refresh();
        _ = RefreshAbilityInfoAsync();
    }

    private static IReadOnlyList<ComboItem> GetAbilityNumberItems() =>
    [
        new("Slot 1", 1),
        new("Slot 2", 2),
        new("Slot H", 4)
    ];

    private void SetDevAbilityNumber(int abilityNumber)
    {
        if (Pokemon is null || AppState?.IsHaXEnabled is not true)
        {
            return;
        }

        Pokemon.AbilityNumber = abilityNumber;
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

        Haptics.Confirm();
        Pokemon.IsNicknamed = false;
        Pokemon.ClearNickname();
    }

    private void AfterFormChanged()
    {
        if (Pokemon is { Species: (ushort)Species.Indeedee })
        {
            Pokemon.SetGender(Pokemon.Form);
        }

        // For Furfrou, auto-set days remaining to the maximum when switching to a trim form via the
        // dropdown, so the form argument is immediately valid (a 0-day trim reverts to Natural).
        if (Pokemon is { Species: (ushort)Species.Furfrou, Form: not 0 })
        {
            var maxDays = FormArgumentUtil.GetFormArgumentMax(Pokemon.Species, Pokemon.Form, Pokemon.Context);
            Pokemon.ChangeFormArgument(maxDays);
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

    private async Task OpenAlcremieEditorDialog()
    {
        var parameters = new DialogParameters<AlcremieEditorDialog> { { x => x.Pokemon, Pokemon } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        var dialog = await DialogService.ShowAsync<AlcremieEditorDialog>("Alcremie Appearance", parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private async Task OpenVivillonEditorDialog()
    {
        var parameters = new DialogParameters<VivillonEditorDialog> { { x => x.Pokemon, Pokemon } };
        var title = Pokemon?.Species switch
        {
            (ushort)Species.Scatterbug => "Scatterbug Pattern",
            (ushort)Species.Spewpa => "Spewpa Pattern",
            _ => "Vivillon Pattern"
        };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        var dialog = await DialogService.ShowAsync<VivillonEditorDialog>(title, parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private async Task OpenFurfrouEditorDialog()
    {
        var parameters = new DialogParameters<FurfrouEditorDialog> { { x => x.Pokemon, Pokemon } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        var dialog = await DialogService.ShowAsync<FurfrouEditorDialog>("Furfrou Trim", parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private async Task OpenPumpkabooSizeDialog()
    {
        var parameters = new DialogParameters<PumpkabooSizeDialog> { { x => x.Pokemon, Pokemon } };
        var title = Pokemon?.Species == (ushort)Species.Gourgeist
            ? "Gourgeist Size"
            : "Pumpkaboo Size";
        var dialog = await DialogService.ShowAsync<PumpkabooSizeDialog>(title, parameters, AppearanceDialogOptions);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private async Task OpenMiniorColorDialog()
    {
        var parameters = new DialogParameters<MiniorColorDialog> { { x => x.Pokemon, Pokemon } };
        var dialog = await DialogService.ShowAsync<MiniorColorDialog>("Minior Form", parameters, AppearanceDialogOptions);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private async Task OpenFlowerColorDialog()
    {
        var parameters = new DialogParameters<FlowerColorDialog> { { x => x.Pokemon, Pokemon } };
        var title = Pokemon?.Species switch
        {
            (ushort)Species.Floette => "Floette Flower Color",
            (ushort)Species.Florges => "Florges Flower Color",
            _ => "Flabébé Flower Color"
        };
        var dialog = await DialogService.ShowAsync<FlowerColorDialog>(title, parameters, AppearanceDialogOptions);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private async Task OpenPidEcDialog()
    {
        var parameters = new DialogParameters<PidEcDialog> { { x => x.Pokemon, Pokemon } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<PidEcDialog>("PID / EC Generator", parameters, options);
    }

    private async Task OpenSpindaPatternDialog()
    {
        var parameters = new DialogParameters<SpindaPatternDialog> { { x => x.Pokemon, Pokemon } };
        var dialog = await DialogService.ShowAsync<SpindaPatternDialog>("Spinda Spot Pattern", parameters, AppearanceDialogOptions);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            AppService.LoadPokemonStats(Pokemon);
            RefreshService.Refresh();
        }
    }

    private Task OpenTrashBytesEditor(StringSource field) =>
        OpenTrashBytesEditorAsync(Pokemon, field);

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
        if (Pokemon.Format > 2 || !string.IsNullOrEmpty(Pokemon.Nickname))
        {
            return;
        }

        // Fallback to default name if nickname couldn't be encoded
        Pokemon.Nickname = defaultName;
        Pokemon.IsNicknamed = false;
    }

    private async Task EvolveAsync()
    {
        if (Pokemon is null)
        {
            return;
        }

        var choices = AppService.GetDirectEvolutions(Pokemon);
        if (choices.Count == 0)
        {
            return;
        }

        EvolutionMethod chosen;
        if (choices.Count == 1)
        {
            chosen = choices[0];
        }
        else
        {
            var parameters = new DialogParameters<EvolvePickerDialog> { { x => x.Choices, choices }, { x => x.Pokemon, Pokemon } };
            var evolveOptions = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
            var dialog = await DialogService.ShowAsync<EvolvePickerDialog>("Choose Evolution", parameters, evolveOptions);
            var result = await dialog.Result;
            if (result is null or { Canceled: true })
            {
                return;
            }

            chosen = (EvolutionMethod)result.Data!;
        }

        // Capture Nincada snapshot before applying (Shedinja side-effect).
        var isNincada = Pokemon.Species == (ushort)Species.Nincada && chosen.Species == (ushort)Species.Ninjask;
        var nincadaSnapshot = isNincada
            ? Pokemon.Clone()
            : null;

        ApplyEvolution(chosen);

        if (isNincada && nincadaSnapshot is not null)
        {
            await OfferShedinjaAsync(nincadaSnapshot);
        }
    }

    private static byte? GetRequiredGender(EvolutionType method) => method switch
    {
        EvolutionType.LevelUpMale or EvolutionType.UseItemMale => 0,
        EvolutionType.LevelUpFemale or EvolutionType.UseItemFemale => 1,
        _ => null
    };

    private void ApplyEvolution(EvolutionMethod method)
    {
        if (Pokemon is null)
        {
            return;
        }

        var destForm = method.GetDestinationForm(Pokemon.Form);

        // Wurmple: match EC/PID to the chosen branch so legality is satisfied.
        if (Pokemon.Species == (ushort)Species.Wurmple)
        {
            var evoGroup = WurmpleUtil.GetWurmpleEvoGroup(method.Species);
            if (Pokemon.Format >= 6)
            {
                // Gen 6+: EC is an independent field — set it to match the branch.
                Pokemon.EncryptionConstant = WurmpleUtil.GetWurmpleEncryptionConstant(evoGroup);
            }
            else
            {
                // Gen 3–5: EC getter returns PID; the EC setter is a no-op, so we must set PID.
                // Note: changing PID may introduce legality flags (gender/nature/ability correlation),
                // which is acceptable in a save editor context.
                uint pid;
                var rnd = Util.Rand;
                do
                {
                    pid = rnd.Rand32();
                } while (evoGroup != WurmpleUtil.GetWurmpleEvoVal(pid));

                Pokemon.PID = pid;
            }
        }

        // Gender-locked evolutions (e.g. Kirlia→Gallade requires male, Combee→Vespiquen requires female).
        // For Gen 3–5, gender is derived from PID, so we must regenerate a PID that satisfies both
        // the required gender and preserves the existing nature/ability correlation where possible.
        var requiredGender = GetRequiredGender(method.Method);
        if (requiredGender is { } targetGender && Pokemon.Gender != targetGender)
        {
            // SetPIDGender re-rolls PID (preserving nature/ability/non-shiny) for Gen ≤ 5,
            // and also updates EC when the PKM originated in Gen 3–5 but is stored in Gen 6+.
            Pokemon.SetPIDGender(targetGender);
            Pokemon.Gender = targetGender;
        }

        // Beauty-based evolutions (e.g. Feebas→Milotic): ensure the contest beauty stat
        // meets the required threshold so the evolution is legal.
        // Only set beauty when the Pokémon can legally have contest stats:
        // - Gen 3–4 origin: always (RSE/DPPt have contests)
        // - Gen 5–6 origin in Gen 6+ format: yes (ORAS contests / transfer)
        // - Gen 8b (BDSP): yes (BD/SP has contests)
        // Gen 7+ origin otherwise cannot have contest stats, but PKHeX's reverse evolution
        // checker skips the beauty requirement, so the evolution chain still validates.
        if (method.Method == EvolutionType.LevelUpBeauty
            && Pokemon.CanHaveContestStats()
            && Pokemon is IContestStats contestStats
            && contestStats.ContestBeauty < method.Argument)
        {
            contestStats.ContestBeauty = (byte)method.Argument;
        }

        // Trade evolutions (e.g. Feebas→Milotic via Prism Scale, Machoke→Machamp):
        // simulate a trade by setting the handling trainer from the save file so the
        // Pokémon is no longer flagged as "untraded".
        if (method.Method.IsTrade
            && Pokemon is IHandlerUpdate
            && Pokemon.IsUntraded
            && AppState.SaveFile is { } sav)
        {
            Pokemon.HandlingTrainerName = sav.OT;
            Pokemon.HandlingTrainerGender = sav.Gender;
            Pokemon.HandlingTrainerFriendship = Pokemon.PersonalInfo.BaseFriendship;
            Pokemon.CurrentHandler = 1;
        }

        // Bump level to the minimum required for this evolution.
        if (method.Level > 0 && Pokemon.CurrentLevel < method.Level)
        {
            Pokemon.CurrentLevel = method.Level;
        }

        // For level-up evolutions the legality check requires current level > met level
        // (specifically: current level ≥ met level + 1).
        // Set directly to MetLevel + 1 so this holds even if CurrentLevel is well below MetLevel.
        // Level 100 Pokémon are exempt — PKHeX allows level-up evolutions at max level.
        if (method.Method.IsLevelUpRequired
            && Pokemon.CurrentLevel <= Pokemon.MetLevel
            && Pokemon.CurrentLevel < Experience.MaxLevel)
        {
            Pokemon.CurrentLevel = (byte)Math.Min(Experience.MaxLevel, Pokemon.MetLevel + 1);
        }

        // Capture before changing species: Gen 3 computes IsNicknamed from Nickname vs. species name,
        // so reading it after the species change gives the wrong answer.
        var wasNicknamed = Pokemon.IsNicknamed;

        Pokemon.Species = method.Species;
        Pokemon.Form = destForm;
        Pokemon.Gender = Pokemon.GetSaneGender();

        // Refresh ability: preserve the ability slot index but update the ability ID
        // to match the new species' ability at that slot (e.g. Feebas slot 0 = Swift Swim
        // → Milotic slot 0 = Marvel Scale).
        var abilityIndex = Pokemon.AbilityNumber switch
        {
            2 => 1,
            4 => 2,
            _ => 0
        };
        Pokemon.RefreshAbility(abilityIndex);

        if (!wasNicknamed)
        {
            Pokemon.ClearNickname();
        }

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void SetSpecies(ushort species)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.Species = species;

        // Reset Form when it is no longer valid for the new species' form list.
        // Without this, switching from a many-form species (e.g. Vivillon Form 19)
        // to one with fewer forms leaves Form pointing past the new form list,
        // producing a blank Form picker and an invalid PKM state.
        if (Pokemon.Form >= Pokemon.PersonalInfo.FormCount)
        {
            Pokemon.Form = 0;
        }

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

    /// <summary>
    /// When Nincada evolves into Ninjask, offers to generate a Shedinja and place it in
    /// the first available party or box slot, mirroring the in-game mechanic.
    /// </summary>
    private async Task OfferShedinjaAsync(PKM nincadaSnapshot)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Generate Shedinja?",
            "Nincada has evolved into Ninjask. In the games, a Shedinja also appears in the next available slot. Would you like to generate one?",
            yesText: "Generate Shedinja",
            noText: "Skip");

        if (confirmed is not true)
        {
            return;
        }

        var shedinja = nincadaSnapshot;
        shedinja.Species = (ushort)Species.Shedinja;

        // If the Nincada wasn't nicknamed, update the cached nickname to "Shedinja".
        if (!shedinja.IsNicknamed)
        {
            shedinja.ClearNickname();
        }

        AppService.LoadPokemonStats(shedinja);

        if (!AppService.TryPlacePokemonInFirstAvailableSlot(shedinja))
        {
            Snackbar.Add("No empty slot available for Shedinja.", Severity.Warning);
            return;
        }

        Snackbar.Add("Shedinja placed in the first available slot.", Severity.Success);
    }
}
