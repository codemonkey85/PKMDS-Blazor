namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class StatsTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    private bool ShowStatsChart { get; set; }

    private bool IsPartyPokemon => Pokemon is not null && HasPersistentPartyStats;

    // Party stats are genuinely persistent (not just computed for display) when:
    // - Gen 1/2: box and party format are identical (SIZE_PARTY == SIZE_STORED)
    // - Let's Go (PB7): party members live in the box
    // - Any game: Pokémon is explicitly selected from a party slot
    // For Gen 3+ box Pokémon, LoadPokemonStats computes stats for display but
    // they aren't saved back to the box format, so we shouldn't offer editing.
    private bool HasPersistentPartyStats =>
        Pokemon?.Format <= 2 ||
        Pokemon is PB7 ||
        AppState?.SelectedPartySlotNumber is not null;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private static string GetCharacteristic(PKM? pokemon) =>
        pokemon?.Characteristic is { } characteristicIndex and > -1 &&
        GameInfo.Strings.characteristics is { Length: > 0 } characteristics &&
        characteristicIndex < characteristics.Length
            ? characteristics[characteristicIndex]
            : string.Empty;

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

        if (AppState?.IsHaXEnabled is not true)
        {
            AppService.LoadPokemonStats(Pokemon);
        }
    }

    private void OnStatNatureSet(Nature statNature)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.StatAlignment = statNature;
        if (AppState?.IsHaXEnabled is not true)
        {
            AppService.LoadPokemonStats(Pokemon);
        }
    }

    private static int GetEvMax(int generation) => generation switch
    {
        1 or 2 => EffortValues.Max12,
        3 or 4 or 5 => EffortValues.Max255,
        _ => EffortValues.Max252
    };

    private string GetIvFieldClass(int iv, Stats stat)
    {
        var maxIv = AppState?.SaveFile?.Generation is 1 or 2
            ? 15
            : 31;
        var maxClass = iv >= maxIv
            ? "stat-maxed"
            : string.Empty;
        var natureClass = stat == Stats.Hp
            ? string.Empty
            : GetStatClass(stat);
        return $"{natureClass} {maxClass}".Trim();
    }

    // Bold threshold for EVs: 252 for Gen 3+ (effective stat-gain cap even when stored max is 255),
    // ushort.MaxValue for Gen 1/2 (EffortValues.Max12).
    private static int GetEvBoldThreshold(int generation) => generation is 1 or 2
        ? EffortValues.Max12
        : EffortValues.Max252;

    private string GetEvFieldClass(int ev, Stats stat, int saveGeneration)
    {
        var maxClass = ev >= GetEvBoldThreshold(saveGeneration)
            ? "stat-maxed"
            : string.Empty;
        var natureClass = stat == Stats.Hp
            ? string.Empty
            : GetStatClass(stat);
        return $"{natureClass} {maxClass}".Trim();
    }

    private string GetStatClass(Stats stat)
    {
        if (Pokemon is null)
        {
            return string.Empty;
        }

        var nature = Pokemon.Format >= 8
            ? Pokemon.StatAlignment
            : Pokemon.Nature;
        return GetNatureModifier(nature, stat) switch
        {
            NatureModifier.Boosted => "plus-nature",
            NatureModifier.Hindered => "minus-nature",
            _ => string.Empty
        };
    }

    private NatureModifier GetNatureModifier(Stats stat)
    {
        if (Pokemon is null)
        {
            return NatureModifier.Neutral;
        }

        var nature = Pokemon.Format >= 8
            ? Pokemon.StatAlignment
            : Pokemon.Nature;
        return GetNatureModifier(nature, stat);
    }

    private string GetNatureAdornmentIcon(Stats stat) =>
        GetNatureAdornmentIcon(GetNatureModifier(stat));

    private Color GetNatureAdornmentColor(Stats stat) =>
        GetNatureAdornmentColor(GetNatureModifier(stat));

    private Adornment GetNatureAdornment(Stats stat) =>
        GetNatureAdornment(GetNatureModifier(stat));

    private string GetNatureTooltip(Stats stat) =>
        GetNatureTooltip(GetNatureModifier(stat));

    private void OnSetDv(Stats stat, int newValue)
    {
        if (Pokemon is not (PK1 or PK2))
        {
            return;
        }

        var saveGeneration = AppState?.SaveFile?.Generation;
        if (saveGeneration is not 1 and not 2)
        {
            return;
        }

        if (Pokemon is PK1 && stat == Stats.Hp)
        {
            return;
        }

        if (newValue < 0)
        {
            newValue = 0;
        }

        if (newValue > 15)
        {
            newValue = 15;
        }

        switch (saveGeneration)
        {
            case 1 when Pokemon is PK1 pk1:
                {
                    switch (stat)
                    {
                        case Stats.Attack:
                            pk1.IV_ATK = (byte)newValue;
                            break;
                        case Stats.Defense:
                            pk1.IV_DEF = (byte)newValue;
                            break;
                        case Stats.Speed:
                            pk1.IV_SPE = (byte)newValue;
                            break;
                        case Stats.Special:
                            pk1.IV_SPC = (byte)newValue;
                            break;
                    }

                    break;
                }
            case 2 when Pokemon is PK2 pk2:
                {
                    switch (stat)
                    {
                        case Stats.Hp:
                            pk2.IV_HP = (byte)newValue;
                            break;
                        case Stats.Attack:
                            pk2.IV_ATK = (byte)newValue;
                            break;
                        case Stats.Defense:
                            pk2.IV_DEF = (byte)newValue;
                            break;
                        case Stats.Speed:
                            pk2.IV_SPE = (byte)newValue;
                            break;
                        case Stats.SpecialAttack:
                            pk2.IV_SPA = (byte)newValue;
                            break;
                        case Stats.SpecialDefense:
                            pk2.IV_SPD = (byte)newValue;
                            break;
                    }

                    break;
                }
        }

        if (AppState?.IsHaXEnabled is not true)
        {
            AppService.LoadPokemonStats(Pokemon);
        }
    }

    private void OnSetIv(Stats stat, int newValue)
    {
        if (Pokemon is null)
        {
            return;
        }

        var saveGeneration = AppState?.SaveFile?.Generation;
        if (saveGeneration is null || saveGeneration == 1 && Pokemon is PK1 && stat == Stats.Hp)
        {
            return;
        }

        if (newValue < 0)
        {
            newValue = 0;
        }

        if (saveGeneration is 1 or 2 &&
            Pokemon is PK1 or PK2 && newValue > 15)
        {
            newValue = 15;
        }
        else if (newValue > 31)
        {
            newValue = 31;
        }

        var statIndex = stat switch
        {
            Stats.Hp => 0,
            Stats.Attack => 1,
            Stats.Defense => 2,
            Stats.Speed => 3,
            Stats.SpecialAttack => 4,
            Stats.SpecialDefense => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };

        Pokemon.SetIV(statIndex, (byte)newValue);

        if (AppState?.IsHaXEnabled is not true)
        {
            AppService.LoadPokemonStats(Pokemon);
        }
    }

    private void OnHaXStatHpSet(int newValue) =>
        ApplyHaXStatHp(Pokemon, AppState?.IsHaXEnabled is true, newValue);

    private void OnHaXStatSet(int newValue, Action<PKM, int> setter) =>
        ApplyHaXStat(Pokemon, AppState?.IsHaXEnabled is true, newValue, setter);

    internal static void ApplyHaXStatHp(PKM? pkm, bool haXEnabled, int newValue)
    {
        if (pkm is null || !haXEnabled)
        {
            return;
        }

        pkm.Stat_HPMax = newValue;
        pkm.Stat_HPCurrent = newValue;
    }

    internal static void ApplyHaXStat(PKM? pkm, bool haXEnabled, int newValue, Action<PKM, int> setter)
    {
        if (pkm is null || !haXEnabled)
        {
            return;
        }

        setter(pkm, newValue);
    }

    private void OnCurrentHpChanged(int value)
    {
        if (Pokemon is null)
        {
            return;
        }

        value = Math.Clamp(value, 0, Pokemon.Stat_HPMax);
        Pokemon.Stat_HPCurrent = value;
    }

    private int GetCurrentStatusValue()
    {
        if (Pokemon is null)
        {
            return 0;
        }

        if (Pokemon.Format <= 4)
        {
            var condition = (StatusCondition)(Pokemon.Status_Condition & 0xFF);
            if (condition == StatusCondition.None)
            {
                return 0;
            }

            if (condition <= StatusCondition.Sleep7)
            {
                return (int)StatusCondition.Sleep1;
            }

            if ((condition & StatusCondition.PoisonBad) != 0)
            {
                return (int)StatusCondition.PoisonBad;
            }

            return (int)condition;
        }

        return Pokemon.Status_Condition & 0xFF;
    }

    private void OnStatusChanged(int value)
    {
        if (Pokemon is null)
        {
            return;
        }

        if (Pokemon.Format <= 4)
        {
            var current = Pokemon.Status_Condition & ~0xFF;
            if (value == (int)StatusCondition.Sleep1)
            {
                var sleepTurns = GetSleepTurns();
                current |= sleepTurns;
            }
            else
            {
                current |= value;
            }

            Pokemon.Status_Condition = current;
        }
        else
        {
            var current = Pokemon.Status_Condition & ~0xFF;
            current |= value;
            Pokemon.Status_Condition = current;
        }
    }

    private int GetSleepTurns()
    {
        if (Pokemon is null)
        {
            return 1;
        }

        var raw = Pokemon.Status_Condition & 7;
        return raw is >= 1 and <= 7
            ? raw
            : 1;
    }

    private void OnSleepTurnsChanged(int value)
    {
        if (Pokemon is null)
        {
            return;
        }

        value = Math.Clamp(value, 1, 7);
        var current = Pokemon.Status_Condition & ~0xFF;
        current |= value;
        Pokemon.Status_Condition = current;
    }

    private List<StatusOption> GetStatusOptions()
    {
        var options = new List<StatusOption>();
        var isGen8a = Pokemon?.Context == EntityContext.Gen8a;

        if (Pokemon is null)
        {
            return options;
        }

        options.Add(new StatusOption(0, "None", null));

        if (Pokemon.Format <= 4)
        {
            options.Add(new StatusOption((int)StatusCondition.Sleep1, "Sleep",
                ImageHelper.GetSleepStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusCondition.Poison, "Poison",
                ImageHelper.GetPoisonStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusCondition.PoisonBad,
                Pokemon.Format is 3 or 4
                    ? "Toxic"
                    : "Badly Poisoned",
                ImageHelper.GetToxicStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusCondition.Burn, "Burn",
                ImageHelper.GetBurnStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusCondition.Freeze, "Freeze",
                ImageHelper.GetFrostbiteStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusCondition.Paralysis, "Paralysis",
                ImageHelper.GetParalysisStatusSpriteFileName()));
        }
        else
        {
            options.Add(new StatusOption((int)StatusType.Paralysis, "Paralysis",
                ImageHelper.GetParalysisStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusType.Sleep,
                isGen8a
                    ? "Drowsy"
                    : "Sleep",
                ImageHelper.GetSleepStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusType.Freeze,
                isGen8a
                    ? "Frostbite"
                    : "Freeze",
                ImageHelper.GetFrostbiteStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusType.Burn, "Burn",
                ImageHelper.GetBurnStatusSpriteFileName()));
            options.Add(new StatusOption((int)StatusType.Poison, "Poison",
                ImageHelper.GetPoisonStatusSpriteFileName()));
        }

        return options;
    }

    private string? GetCurrentStatusSpriteFileName() =>
        ImageHelper.GetStatusOverlaySpriteFileName(Pokemon);

    private bool IsSleepSelected()
    {
        if (Pokemon is null)
        {
            return false;
        }

        if (Pokemon.Format <= 4)
        {
            var condition = (StatusCondition)(Pokemon.Status_Condition & 0xFF);
            return condition is >= StatusCondition.Sleep1 and <= StatusCondition.Sleep7;
        }

        return false;
    }

    private record StatusOption(int Value, string Label, string? SpriteFileName);
}
