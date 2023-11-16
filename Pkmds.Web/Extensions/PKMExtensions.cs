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
}
