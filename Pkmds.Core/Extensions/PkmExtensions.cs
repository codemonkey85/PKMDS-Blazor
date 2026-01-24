namespace Pkmds.Core.Extensions;

public static class PkmExtensions
{
    public static bool IsValidSpecies(this ushort speciesId) =>
        speciesId is > (ushort)Species.None and < (ushort)Species.MAX_COUNT;

    public static bool IsValidSpecies(this ushort? speciesId) =>
        speciesId is { } species && species.IsValidSpecies();

    public static bool IsInvalidSpecies(this ushort speciesId) => !speciesId.IsValidSpecies();

    public static bool IsInvalidSpecies(this ushort? speciesId) => !speciesId.IsValidSpecies();

    extension(PKM pkm)
    {
        public uint? GetFormArgument(uint? valueIfNull = null) =>
            (pkm as IFormArgument)?.FormArgument ?? valueIfNull;

        public (byte Type1, byte Type2) GetGenerationTypes()
        {
            var type1 = pkm.PersonalInfo.Type1;
            var type2 = pkm.PersonalInfo.Type2;
            var generation = pkm.Generation;

            return generation <= 2
                ? (ConvertGenerationType(type1, generation), ConvertGenerationType(type2, generation))
                : (type1, type2);

            static byte ConvertGenerationType(byte type, byte generation) =>
                (byte)((MoveType)type).GetMoveTypeGeneration(generation);
        }

        public int GetMarking(int index)
        {
            if (pkm is not IAppliedMarkings appliedMarkings)
            {
                throw new Exception("Pokémon does not implement IAppliedMarkings");
            }

            if ((uint)index >= appliedMarkings.MarkingCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return pkm switch
            {
                IAppliedMarkings<bool> b => b.GetMarking(index)
                    ? 1
                    : 0,
                IAppliedMarkings<MarkingColor> c => (int)c.GetMarking(index),
                _ => throw new Exception("Pokémon does not implement IAppliedMarkings")
            };
        }

        // ReSharper disable once InconsistentNaming
        public ReadOnlyCollection<int> GetPP() => new(
        [
            pkm.Move1_PP,
            pkm.Move2_PP,
            pkm.Move3_PP,
            pkm.Move4_PP
        ]);

        // ReSharper disable once InconsistentNaming
        public ReadOnlyCollection<int> GetPPUps() => new(
        [
            pkm.Move1_PPUps,
            pkm.Move2_PPUps,
            pkm.Move3_PPUps,
            pkm.Move4_PPUps
        ]);

        // ReSharper disable once InconsistentNaming
        public void SetPP(int moveIndex, int pp)
        {
            if (pp < 0)
            {
                pp = 0;
            }

            switch (moveIndex)
            {
                case 0:
                    pkm.Move1_PP = pp;
                    break;
                case 1:
                    pkm.Move2_PP = pp;
                    break;
                case 2:
                    pkm.Move3_PP = pp;
                    break;
                case 3:
                    pkm.Move4_PP = pp;
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        public void SetPPUps(int moveIndex, int ppUps)
        {
            if (ppUps < 0)
            {
                ppUps = 0;
            }

            switch (moveIndex)
            {
                case 0:
                    pkm.Move1_PPUps = ppUps;
                    break;
                case 1:
                    pkm.Move2_PPUps = ppUps;
                    break;
                case 2:
                    pkm.Move3_PPUps = ppUps;
                    break;
                case 3:
                    pkm.Move4_PPUps = ppUps;
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        public int GetMaxPP(int moveIndex)
        {
            var move = pkm.GetMove(moveIndex);
            // ReSharper disable once InconsistentNaming
            var moveBasePP = MoveInfo.GetPP(pkm.Context, move);
            var ppUps = pkm.GetPPUps()[moveIndex];

            return moveBasePP + moveBasePP * ppUps / 5;
        }

        public bool GetIsShinySafe()
        {
            if (pkm.Format <= 2) // Gen I / II
            {
                // In Gen I / II, shininess is determined by the DV values
                return pkm switch
                {
                    PK1 pk1 => GetIsShinyGb(pk1.DV16),
                    PK2 pk2 => GetIsShinyGb(pk2.DV16),
                    _ => false
                };
            }

            // For Gen III and later, shininess is determined by the PID
            return pkm.IsShiny;

            static bool GetIsShinyGb(ushort dv16) => (dv16 & 0x2FFF) == 0x2AAA;
        }

        public void SetIsShinySafe(bool shiny)
        {
            if (pkm.Format > 2) // Gen III+
            {
                pkm.SetIsShiny(shiny);
                return;
            }

            if (shiny)
            {
                pkm.SetIsShiny(true);
                return;
            }

            if (!pkm.IsShiny)
            {
                return;
            }

            do
            {
                pkm.SetRandomIVs();
            } while (pkm.GetIsShinySafe());
        }
    }
}
