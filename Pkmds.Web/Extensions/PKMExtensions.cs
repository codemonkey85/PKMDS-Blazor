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

        static byte ConvertGenerationType(byte type, byte generation) => (byte)((MoveType)type).GetMoveTypeGeneration(generation);
    }

    public static int GetMarking(this PKM pokemon, int index)
    {
        if (pokemon is not IAppliedMarkings appliedMarkings)
        {
            throw new ArgumentException("Pokémon does not implement IAppliedMarkings", nameof(pokemon));
        }

        if ((uint)index >= appliedMarkings.MarkingCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return pokemon switch
        {
            IAppliedMarkings<bool> b => b.GetMarking(index) ? 1 : 0,
            IAppliedMarkings<MarkingColor> c => (int)c.GetMarking(index),
            _ => throw new ArgumentException("Pokémon does not implement IAppliedMarkings", nameof(pokemon)),
        };
    }

    public static ReadOnlyCollection<int> GetPP(this PKM pokemon) => new(
        [
            pokemon.Move1_PP,
            pokemon.Move2_PP,
            pokemon.Move3_PP,
            pokemon.Move4_PP
        ]);

    public static ReadOnlyCollection<int> GetPPUps(this PKM pokemon) => new(
        [
            pokemon.Move1_PPUps,
            pokemon.Move2_PPUps,
            pokemon.Move3_PPUps,
            pokemon.Move4_PPUps
        ]);

    public static void SetPP(this PKM pokemon, int moveIndex, int pp)
    {
        if (pokemon is null)
        {
            return;
        }

        if(pp < 0)
        {
            pp = 0;
        }

        switch (moveIndex)
        {
            case 0:
                pokemon.Move1_PP = pp;
                break;
            case 1:
                pokemon.Move2_PP = pp;
                break;
            case 2:
                pokemon.Move3_PP = pp;
                break;
            case 3:
                pokemon.Move4_PP = pp;
                break;
        }
    }

    public static void SetPPUps(this PKM pokemon, int moveIndex, int ppUps)
    {
        if (pokemon is null)
        {
            return;
        }

        if (ppUps < 0)
        {
            ppUps = 0;
        }

        switch (moveIndex)
        {
            case 0:
                pokemon.Move1_PPUps = ppUps;
                break;
            case 1:
                pokemon.Move2_PPUps = ppUps;
                break;
            case 2:
                pokemon.Move3_PPUps = ppUps;
                break;
            case 3:
                pokemon.Move4_PPUps = ppUps;
                break;
        }
    }

    public static int GetMaxPP(this PKM pokemon, int moveIndex) 
    {
        var move = pokemon.GetMove(moveIndex);
        var moveBasePP = MoveInfo.GetPP(pokemon.Context, move);
        var ppUps = pokemon.GetPPUps()[moveIndex];

        return moveBasePP + moveBasePP * ppUps / 5;
    }
}
