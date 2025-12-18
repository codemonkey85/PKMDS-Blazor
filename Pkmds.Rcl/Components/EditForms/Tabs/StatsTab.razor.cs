namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class StatsTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

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

    private static int GetEvMax(int generation) => generation switch
    {
        1 or 2 => EffortValues.Max12,
        3 or 4 or 5 => EffortValues.Max255,
        _ => EffortValues.Max252
    };

    private string GetStatClass(Stats stat)
    {
        if (Pokemon is null)
        {
            return string.Empty;
        }

        var nature = Pokemon.Format >= 8
            ? Pokemon.StatNature
            : Pokemon.Nature;

        var (up, dn) = NatureAmp.GetNatureModification(nature);

        return up == dn
            ? string.Empty
            : up == (int)stat
                ? "plus-nature"
                : dn == (int)stat
                    ? "minus-nature"
                    : string.Empty;
    }

    private static string GetTeraTypeDisplayName(byte teraTypeId) => teraTypeId == TeraTypeUtil.Stellar
        ? GameInfo.Strings.Types[TeraTypeUtil.StellarTypeDisplayStringIndex]
        : GameInfo.Strings.Types[teraTypeId];

    private void OnSetDv(Stats stat, int newValue)
    {
        if (Pokemon is not PK1 or PK2)
        {
            return;
        }

        var saveGeneration = AppState?.SaveFile?.Generation;
        if (saveGeneration is not 1 or 2)
        {
            return;
        }

        if (Pokemon is PK1 && stat == Stats.HP)
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

        if (saveGeneration == 1 && Pokemon is PK1 pk1)
        {
            if (stat == Stats.Attack)
            {
                pk1.IV_ATK = (byte)newValue;
            }

            if (stat == Stats.Defense)
            {
                pk1.IV_DEF = (byte)newValue;
            }

            if (stat == Stats.Speed)
            {
                pk1.IV_SPE = (byte)newValue;
            }

            if (stat == Stats.Special)
            {
                pk1.IV_SPC = (byte)newValue;
            }
        }

        if (saveGeneration == 2 && Pokemon is PK2 pk2)
        {
            if (stat == Stats.HP)
            {
                pk2.IV_HP = (byte)newValue;
            }

            if (stat == Stats.Attack)
            {
                pk2.IV_ATK = (byte)newValue;
            }

            if (stat == Stats.Defense)
            {
                pk2.IV_DEF = (byte)newValue;
            }

            if (stat == Stats.Speed)
            {
                pk2.IV_SPE = (byte)newValue;
            }

            if (stat == Stats.SpecialAttack)
            {
                pk2.IV_SPA = (byte)newValue;
            }

            if (stat == Stats.SpecialDefense)
            {
                pk2.IV_SPD = (byte)newValue;
            }
        }

        AppService.LoadPokemonStats(Pokemon);
    }

    private void OnSetIv(Stats stat, int newValue)
    {
        if (Pokemon is null)
        {
            return;
        }

        var saveGeneration = AppState?.SaveFile?.Generation;
        if (saveGeneration is null)
        {
            return;
        }

        if (saveGeneration == 1 && Pokemon is PK1 && stat == Stats.HP)
        {
            return;
        }

        if (newValue < 0)
        {
            newValue = 0;
        }

        if ((saveGeneration == 1 || saveGeneration == 2) &&
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
            Stats.HP => 0,
            Stats.Attack => 1,
            Stats.Defense => 2,
            Stats.Speed => 3,
            Stats.SpecialAttack => 4,
            Stats.SpecialDefense => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };

        Pokemon.SetIV(statIndex, (byte)newValue);

        AppService.LoadPokemonStats(Pokemon);
    }

    private enum Stats
    {
        HP = -1,
        Attack = 0,
        Defense,
        Speed,
        SpecialAttack,
        SpecialDefense,
        Special = 99
    }
}
