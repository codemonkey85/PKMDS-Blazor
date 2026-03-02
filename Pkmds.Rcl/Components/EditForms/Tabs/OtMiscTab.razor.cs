namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class OtMiscTab : IDisposable
{
    // Memory ID → MemoryArgType mapping (based on PKHeX.Core memory text format strings)
    private static readonly MemoryArgType[] MemoryArgTypes =
    [
        // 0-9
        MemoryArgType.None, MemoryArgType.SpecificLocation, MemoryArgType.SpecificLocation, MemoryArgType.GeneralLocation,
        MemoryArgType.GeneralLocation, MemoryArgType.Item, MemoryArgType.None, MemoryArgType.Species,
        MemoryArgType.None, MemoryArgType.Item,
        // 10-19
        MemoryArgType.None, MemoryArgType.None, MemoryArgType.Move, MemoryArgType.Species,
        MemoryArgType.Species, MemoryArgType.Item, MemoryArgType.Item, MemoryArgType.Species,
        MemoryArgType.Species, MemoryArgType.SpecificLocation,
        // 20-29
        MemoryArgType.None, MemoryArgType.Species, MemoryArgType.None, MemoryArgType.None,
        MemoryArgType.SpecificLocation, MemoryArgType.Species, MemoryArgType.Item, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.Species,
        // 30-39
        MemoryArgType.None, MemoryArgType.GeneralLocation, MemoryArgType.GeneralLocation, MemoryArgType.GeneralLocation,
        MemoryArgType.Item, MemoryArgType.Move, MemoryArgType.Move, MemoryArgType.GeneralLocation,
        MemoryArgType.GeneralLocation, MemoryArgType.GeneralLocation,
        // 40-49
        MemoryArgType.Item, MemoryArgType.None, MemoryArgType.GeneralLocation, MemoryArgType.None,
        MemoryArgType.Species, MemoryArgType.Species, MemoryArgType.None, MemoryArgType.None,
        MemoryArgType.Move, MemoryArgType.Move,
        // 50-59
        MemoryArgType.Species, MemoryArgType.Item, MemoryArgType.Item, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.None, MemoryArgType.None, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.GeneralLocation,
        // 60-69
        MemoryArgType.Species, MemoryArgType.None, MemoryArgType.None, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.None, MemoryArgType.None, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.None,
        // 70-79
        MemoryArgType.GeneralLocation, MemoryArgType.Species, MemoryArgType.Species, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.Species, MemoryArgType.None, MemoryArgType.None,
        MemoryArgType.None, MemoryArgType.None,
        // 80-89
        MemoryArgType.Move, MemoryArgType.Move, MemoryArgType.Species, MemoryArgType.Species,
        MemoryArgType.Item, MemoryArgType.None, MemoryArgType.GeneralLocation, MemoryArgType.Species,
        MemoryArgType.Item, MemoryArgType.Move
    ];

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    private List<ComboItem>? CachedMemoryItems { get; set; }
    private List<ComboItem>? CachedFeelingItems { get; set; }
    private List<ComboItem>? CachedQualityItems { get; set; }
    private List<ComboItem>? CachedAffixedRibbonItems { get; set; }
    private List<ComboItem>? CachedGeoCountries { get; set; }

    /// <summary>
    /// Returns the memory generation (6 or 8) used for feeling/argument lookups.
    /// Gen 6/7 Pokémon use generation 6 memory sets; Gen 8+ use generation 8 memory sets.
    /// </summary>
    private int MemoryGen => Pokemon?.Context switch
    {
        EntityContext.Gen6 or EntityContext.Gen7 => 6,
        _ => 8
    };

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

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
        RefreshService.OnAppStateChanged += StateHasChanged;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Pokemon is IMemoryOT or IMemoryHT)
        {
            CachedMemoryItems = AppService.GetMemoryComboItems().ToList();
            CachedFeelingItems = AppService.GetMemoryFeelingComboItems(MemoryGen).ToList();
            CachedQualityItems = AppService.GetMemoryQualityComboItems().ToList();
        }
        else
        {
            CachedMemoryItems = null;
            CachedFeelingItems = null;
            CachedQualityItems = null;
        }

        CachedAffixedRibbonItems = Pokemon is IRibbonSetAffixed ? BuildAffixedRibbonItems() : null;
        CachedGeoCountries = Pokemon is IGeoTrack or IRegionOrigin
            ? AppService.GetGeoCountryComboItems().ToList()
            : null;
    }

    private static MemoryArgType GetMemoryArgType(byte memoryId) =>
        memoryId < MemoryArgTypes.Length
            ? MemoryArgTypes[memoryId]
            : MemoryArgType.None;

    private static string GetMemoryArgLabel(MemoryArgType argType) => argType switch
    {
        MemoryArgType.Species => "Pokémon",
        MemoryArgType.Move => "Move",
        MemoryArgType.Item => "Item",
        MemoryArgType.GeneralLocation or MemoryArgType.SpecificLocation => "Location",
        _ => "Variable"
    };

    /// <summary>
    /// Builds the fully-formatted memory preview string, substituting all placeholders.
    /// </summary>
    private string FormatMemoryText(
        byte memoryId,
        ushort variable,
        byte intensity,
        byte feeling,
        string trainerName,
        IEnumerable<ComboItem>? memoryItems,
        IEnumerable<ComboItem>? qualityItems,
        IEnumerable<ComboItem>? feelingItems)
    {
        if (memoryItems is null)
        {
            return string.Empty;
        }

        if (qualityItems is null)
        {
            return string.Empty;
        }

        if (feelingItems is null)
        {
            return string.Empty;
        }

        var template = memoryItems.FirstOrDefault(m => m.Value == memoryId)?.Text ?? string.Empty;
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var argType = GetMemoryArgType(memoryId);
        string variableText;
        if (argType != MemoryArgType.None)
        {
            var argMatch = AppService.GetMemoryArgumentComboItems(argType, MemoryGen).FirstOrDefault(i => i.Value == variable);
            variableText = argMatch?.Text ?? variable.ToString() ?? string.Empty;
        }
        else
        {
            variableText = string.Empty;
        }

        var qualityText = qualityItems.FirstOrDefault(q => q.Value == intensity)?.Text ?? string.Empty;
        var feelingText = feelingItems.FirstOrDefault(f => f.Value == feeling)?.Text ?? string.Empty;
        var pokemonName = AppService.GetPokemonSpeciesName(Pokemon?.Species ?? 0) ?? Pokemon?.Nickname ?? string.Empty;

        return template
            .Replace("{0}", pokemonName)
            .Replace("{1}", trainerName)
            .Replace("{2}", variableText)
            .Replace("{3}", feelingText)
            .Replace("{4}", qualityText);
    }

    private void ClearOtMemory()
    {
        if (Pokemon is not IMemoryOT otMemory)
        {
            return;
        }

        otMemory.OriginalTrainerMemory = 0;
        otMemory.OriginalTrainerMemoryIntensity = 0;
        otMemory.OriginalTrainerMemoryFeeling = 0;
        otMemory.OriginalTrainerMemoryVariable = 0;
    }

    private void ClearHtMemory()
    {
        if (Pokemon is not IMemoryHT htMemory)
        {
            return;
        }

        htMemory.HandlingTrainerMemory = 0;
        htMemory.HandlingTrainerMemoryIntensity = 0;
        htMemory.HandlingTrainerMemoryFeeling = 0;
        htMemory.HandlingTrainerMemoryVariable = 0;
    }

    private void ClearGeoData(IGeoTrack geoTrack) =>
        geoTrack.ClearGeoLocationData();

    private static byte GetGeoCountry(IGeoTrack geo, int slot) => slot switch
    {
        1 => geo.Geo1_Country,
        2 => geo.Geo2_Country,
        3 => geo.Geo3_Country,
        4 => geo.Geo4_Country,
        5 => geo.Geo5_Country,
        _ => 0
    };

    private static byte GetGeoRegion(IGeoTrack geo, int slot) => slot switch
    {
        1 => geo.Geo1_Region,
        2 => geo.Geo2_Region,
        3 => geo.Geo3_Region,
        4 => geo.Geo4_Region,
        5 => geo.Geo5_Region,
        _ => 0
    };

    private static void SetGeoCountry(IGeoTrack geo, int slot, byte value)
    {
        switch (slot)
        {
            case 1:
                geo.Geo1_Country = value;
                geo.Geo1_Region = 0;
                break;
            case 2:
                geo.Geo2_Country = value;
                geo.Geo2_Region = 0;
                break;
            case 3:
                geo.Geo3_Country = value;
                geo.Geo3_Region = 0;
                break;
            case 4:
                geo.Geo4_Country = value;
                geo.Geo4_Region = 0;
                break;
            case 5:
                geo.Geo5_Country = value;
                geo.Geo5_Region = 0;
                break;
        }
    }

    private static void SetGeoRegion(IGeoTrack geo, int slot, byte value)
    {
        switch (slot)
        {
            case 1: geo.Geo1_Region = value; break;
            case 2: geo.Geo2_Region = value; break;
            case 3: geo.Geo3_Region = value; break;
            case 4: geo.Geo4_Region = value; break;
            case 5: geo.Geo5_Region = value; break;
        }
    }

    private void FillFromGame()
    {
        if (Pokemon is null || AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        Pokemon.OriginalTrainerName = saveFile.OT;
        Pokemon.OriginalTrainerGender = saveFile.Gender;

        var format = saveFile.GetTrainerIDFormat();
        switch (format)
        {
            case TrainerIDFormat.SixteenBitSingle: // Gen 1-2
                //Pokemon.SetTrainerID16(saveFile.TID);
                break;
            case TrainerIDFormat.SixteenBit: // Gen 3-6
                Pokemon.TID16 = saveFile.TID16;
                Pokemon.SID16 = saveFile.SID16;
                break;
            case TrainerIDFormat.SixDigit: // Gen 7+
                Pokemon.SetTrainerTID7(saveFile.TrainerTID7);
                Pokemon.SetTrainerSID7(saveFile.TrainerSID7);
                break;
        }
    }

    private void OnGenderToggle(Gender newGender) => Pokemon?.OriginalTrainerGender = (byte)newGender;

    private void SetPokemonOtName(string newOtName)
    {
        if (Pokemon is null || AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        if (newOtName is not { Length: > 0 })
        {
            newOtName = saveFile.OT;
        }

        if (newOtName is not { Length: > 0 })
        {
            return;
        }

        Pokemon.OriginalTrainerName = newOtName;

        // For Gen I/II, verify the OT name was set correctly
        // If it becomes empty, the characters were not valid for the Pokémon's language/encoding
        if (Pokemon.Format <= 2 && string.IsNullOrEmpty(Pokemon.OriginalTrainerName) && newOtName.Length > 0)
        {
            // Fallback to save file's OT name if couldn't be encoded
            Pokemon.OriginalTrainerName = saveFile.OT;
        }
    }

    private void SetPokemonEc(uint newEc)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.EncryptionConstant = newEc;

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void RandomizeEc()
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetRandomEC();
        SetPokemonEc(Pokemon.EncryptionConstant);
    }

    private void SetPokemonEc(string newEcHex)
    {
        if (Pokemon is null || !uint.TryParse(newEcHex, NumberStyles.HexNumber, null, out var parsedEc))
        {
            return;
        }

        Pokemon.EncryptionConstant = parsedEc;

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void SetPokemonHomeTracker(string newTrackerHex)
    {
        if (Pokemon is not IHomeTrack homeTrack ||
            !ulong.TryParse(newTrackerHex, NumberStyles.HexNumber, null, out var parsedTracker))
        {
            return;
        }

        homeTrack.Tracker = parsedTracker;
    }

    private static void SetRegionOriginCountry(IRegionOrigin regionOrigin, byte value)
    {
        regionOrigin.Country = value;
        regionOrigin.Region = 0; // reset sub-region when country changes
    }

    /// <summary>
    /// Builds the affixed ribbon combo items for the given Pokémon's format.
    /// Includes "None" (-1) and every ribbon/mark slot that can be affixed (i.e.
    /// any ribbon/mark whose name can be mapped to a <see cref="RibbonIndex"/>),
    /// regardless of whether the Pokémon currently has each ribbon. This ensures
    /// the selector is always fully populated and does not go stale when ribbons
    /// are toggled on the Ribbons tab.
    /// </summary>
    private List<ComboItem> BuildAffixedRibbonItems()
    {
        var items = new List<ComboItem> { new("None", -1) };
        if (Pokemon is null)
        {
            return items;
        }

        foreach (var info in RibbonHelper.GetAllRibbonInfo(Pokemon))
        {
            // Strip the correct prefix so the remainder maps to a RibbonIndex entry.
            // Count-based ribbon properties (e.g. RibbonCountMemoryContest) must strip
            // "RibbonCount" first; boolean ones strip "Ribbon". Anything else is tried as-is.
            var shortName = info.Name;
            if (shortName.StartsWith("RibbonCount", StringComparison.Ordinal))
            {
                shortName = shortName["RibbonCount".Length..];
            }
            else if (shortName.StartsWith("Ribbon", StringComparison.Ordinal))
            {
                shortName = shortName["Ribbon".Length..];
            }

            if (!Enum.TryParse<RibbonIndex>(shortName, ignoreCase: true, out var idx))
            {
                continue;
            }

            var displayName = RibbonHelper.GetRibbonDisplayName(info.Name);
            items.Add(new ComboItem(displayName, (int)idx));
        }

        return items;
    }
}
