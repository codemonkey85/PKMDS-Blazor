namespace Pkmds.Rcl.Models;

/// <summary>
///     Stat indices used for nature modifier calculations and display.
///     Values 0–4 correspond to the indices returned by <see cref="PKHeX.Core.NatureAmp.GetNatureModification" />.
/// </summary>
public enum Stats
{
    Hp = -1,
    Attack = 0,
    Defense,
    Speed,
    SpecialAttack,
    SpecialDefense,
    Special = 99
}
