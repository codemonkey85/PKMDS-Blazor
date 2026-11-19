namespace Pkmds.Rcl;

/// <summary>
/// Partial class for status condition icon sprite filename generation.
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Gets the sprite filename for a burn status icon.</summary>
    public static string GetBurnStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickburn.png";

    /// <summary>Gets the sprite filename for a faint status icon.</summary>
    public static string GetFaintStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickfaint.png";

    /// <summary>Gets the sprite filename for a frostbite status icon.</summary>
    public static string GetFrostbiteStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickfrostbite.png";

    /// <summary>Gets the sprite filename for a paralysis status icon.</summary>
    public static string GetParalysisStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickparalyze.png";

    /// <summary>Gets the sprite filename for a poison status icon.</summary>
    public static string GetPoisonStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickpoison.png";

    /// <summary>Gets the sprite filename for a sleep status icon.</summary>
    public static string GetSleepStatusSpriteFileName() =>
        $"{SpritesRoot}status/sicksleep.png";

    /// <summary>Gets the sprite filename for a toxic poison status icon.</summary>
    public static string GetToxicStatusSpriteFileName() =>
        $"{SpritesRoot}status/sicktoxic.png";

    private static StatusKind GetStatusKind(PKM pokemon)
    {
        if (pokemon.Format > 4)
        {
            return (StatusType)(pokemon.Status_Condition & 0xFF) switch
            {
                StatusType.None => StatusKind.None,
                StatusType.Sleep => StatusKind.Sleep,
                StatusType.Poison => StatusKind.Poison,
                StatusType.Burn => StatusKind.Burn,
                StatusType.Freeze => StatusKind.Freeze,
                StatusType.Paralysis => StatusKind.Paralysis,
                _ => StatusKind.None
            };
        }

        var condition = (StatusCondition)(pokemon.Status_Condition & 0xFF);
        return condition switch
        {
            StatusCondition.None => StatusKind.None,
            <= StatusCondition.Sleep7 => StatusKind.Sleep,
            _ when (condition & StatusCondition.PoisonBad) != 0 => StatusKind.Toxic,
            _ when (condition & StatusCondition.Poison) != 0 => StatusKind.Poison,
            _ when (condition & StatusCondition.Burn) != 0 => StatusKind.Burn,
            _ when (condition & StatusCondition.Freeze) != 0 => StatusKind.Freeze,
            _ when (condition & StatusCondition.Paralysis) != 0 => StatusKind.Paralysis,
            _ => StatusKind.None
        };

    }

    private static string? GetStatusSpriteFileName(StatusKind kind) => kind switch
    {
        StatusKind.None => null,
        StatusKind.Faint => GetFaintStatusSpriteFileName(),
        StatusKind.Sleep => GetSleepStatusSpriteFileName(),
        StatusKind.Poison => GetPoisonStatusSpriteFileName(),
        StatusKind.Toxic => GetToxicStatusSpriteFileName(),
        StatusKind.Burn => GetBurnStatusSpriteFileName(),
        StatusKind.Freeze => GetFrostbiteStatusSpriteFileName(),
        StatusKind.Paralysis => GetParalysisStatusSpriteFileName(),
        _ => null
    };

    public static string? GetStatusOverlaySpriteFileName(PKM? pokemon)
    {
        if (pokemon is not { Species: > 0 } || !pokemon.PartyStatsPresent)
        {
            return null;
        }

        if (pokemon.Stat_HPCurrent == 0)
        {
            return GetFaintStatusSpriteFileName();
        }

        return GetStatusSpriteFileName(GetStatusKind(pokemon));
    }
}

public enum StatusKind
{
    None,
    Faint,
    Sleep,
    Poison,
    Toxic,
    Burn,
    Freeze,
    Paralysis
}
