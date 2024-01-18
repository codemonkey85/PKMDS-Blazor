namespace Pkmds.Web.Extensions;

public static class PKMExtensions
{
    public static uint? GetFormArgument(this PKM pkm, uint? valueIfNull = null) =>
        (pkm as IFormArgument)?.FormArgument ?? valueIfNull;

    public static (byte Type1, byte Type2) GetGenerationTypes(this PKM pkm)
    {
        var type1 = pkm.PersonalInfo.Type1;
        var type2 = pkm.PersonalInfo.Type2;
        var generation = pkm.Generation;

        return generation <= 2
            ? (ConvertGenerationType(type1, generation), ConvertGenerationType(type2, generation))
            : (type1, type2);

        static byte ConvertGenerationType(byte type, int generation) => (byte)((MoveType)type).GetMoveTypeGeneration(generation);
    }

    public static int GetMarking(this PKM pokemon, int index)
    {
        if (pokemon is not IAppliedMarkings appliedMarkings)
        {
            throw new ArgumentException("Pokemon does not implement IAppliedMarkings", nameof(pokemon));
        }

        if ((uint)index >= appliedMarkings.MarkingCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return pokemon switch
        {
            IAppliedMarkings7 markings7 => (int)markings7.GetMarking(index),
            IAppliedMarkings4 markings4 => markings4.GetMarking(index) ? 0 : 1,
            IAppliedMarkings3 markings3 => markings3.GetMarking(index) ? 0 : 1,
            _ => throw new ArgumentException("Pokemon does not implement IAppliedMarkings", nameof(pokemon)),
        };
    }
}
