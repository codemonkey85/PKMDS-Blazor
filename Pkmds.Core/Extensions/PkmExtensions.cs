namespace Pkmds.Core.Extensions;

/// <summary>
/// Extension methods for PKM (Pokémon) objects and related types.
/// Provides utilities for species validation, type handling, markings, moves, shininess, and more.
/// </summary>
public static class PkmExtensions
{
    /// <summary>
    /// Determines whether a species ID is valid (greater than None and less than MAX_COUNT).
    /// </summary>
    /// <param name="speciesId">The species ID to validate.</param>
    /// <returns>True if the species ID is valid; otherwise, false.</returns>
    public static bool IsValidSpecies(this ushort speciesId) =>
        speciesId is > (ushort)Species.None and < (ushort)Species.MAX_COUNT;

    /// <summary>
    /// Determines whether a nullable species ID is valid.
    /// </summary>
    /// <param name="speciesId">The nullable species ID to validate.</param>
    /// <returns>True if the species ID has a value and is valid; otherwise, false.</returns>
    public static bool IsValidSpecies(this ushort? speciesId) =>
        speciesId is { } species && species.IsValidSpecies();

    /// <summary>
    /// Determines whether a species ID is invalid (inverse of <see cref="IsValidSpecies(ushort)" />).
    /// </summary>
    /// <param name="speciesId">The species ID to validate.</param>
    /// <returns>True if the species ID is invalid; otherwise, false.</returns>
    public static bool IsInvalidSpecies(this ushort speciesId) => !speciesId.IsValidSpecies();

    /// <summary>
    /// Determines whether a nullable species ID is invalid.
    /// </summary>
    /// <param name="speciesId">The nullable species ID to validate.</param>
    /// <returns>True if the species ID is null or invalid; otherwise, false.</returns>
    public static bool IsInvalidSpecies(this ushort? speciesId) => !speciesId.IsValidSpecies();

    extension(PKM pkm)
    {
        /// <summary>
        /// Gets the FormArgument value for Pokémon that implement IFormArgument (e.g., Alcremie).
        /// </summary>
        /// <param name="valueIfNull">The value to return if the Pokémon doesn't implement IFormArgument.</param>
        /// <returns>The FormArgument value, or the specified default value.</returns>
        public uint? GetFormArgument(uint? valueIfNull = null) =>
            (pkm as IFormArgument)?.FormArgument ?? valueIfNull;

        /// <summary>
        /// Gets the type(s) of this Pokémon, converting Gen 1/2 type IDs to modern equivalents if needed.
        /// In Gen 1/2, some type IDs differ from later generations due to the introduction of Dark and Steel types.
        /// </summary>
        /// <returns>A tuple containing the primary and secondary type IDs.</returns>
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

        /// <summary>
        /// Gets the value of a specific marking for this Pokémon.
        /// Different generations use different marking systems (boolean or color-based).
        /// </summary>
        /// <param name="index">The 0-based marking index.</param>
        /// <returns>The marking value (0 for unmarked/false, 1+ for marked/color value).</returns>
        /// <exception cref="Exception">Thrown if the Pokémon doesn't implement IAppliedMarkings.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
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

        /// <summary>
        /// Gets the current PP (Power Points) for all four move slots.
        /// </summary>
        /// <returns>A read-only collection of PP values for moves 1-4.</returns>
        // ReSharper disable once InconsistentNaming
        public ReadOnlyCollection<int> GetPP() => new(
        [
            pkm.Move1_PP,
            pkm.Move2_PP,
            pkm.Move3_PP,
            pkm.Move4_PP
        ]);

        /// <summary>
        /// Gets the PP Ups (Power Point upgrades) for all four move slots.
        /// Each PP Up increases a move's max PP by 20% of its base PP.
        /// </summary>
        /// <returns>A read-only collection of PP Up values (0-3) for moves 1-4.</returns>
        // ReSharper disable once InconsistentNaming
        public ReadOnlyCollection<int> GetPPUps() => new(
        [
            pkm.Move1_PPUps,
            pkm.Move2_PPUps,
            pkm.Move3_PPUps,
            pkm.Move4_PPUps
        ]);

        /// <summary>
        /// Sets the current PP for a specific move slot.
        /// </summary>
        /// <param name="moveIndex">The move slot index (0-3).</param>
        /// <param name="pp">The PP value to set (will be clamped to 0 if negative).</param>
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

        /// <summary>
        /// Sets the PP Ups for a specific move slot.
        /// </summary>
        /// <param name="moveIndex">The move slot index (0-3).</param>
        /// <param name="ppUps">The PP Ups value to set (will be clamped to 0 if negative).</param>
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

        /// <summary>
        /// Calculates the maximum PP for a move slot based on the move's base PP and PP Ups applied.
        /// Formula: maxPP = basePP + (basePP * ppUps / 5)
        /// </summary>
        /// <param name="moveIndex">The move slot index (0-3).</param>
        /// <returns>The maximum PP for the move.</returns>
        // ReSharper disable once InconsistentNaming
        public int GetMaxPP(int moveIndex)
        {
            var move = pkm.GetMove(moveIndex);
            // ReSharper disable once InconsistentNaming
            var moveBasePP = MoveInfo.GetPP(pkm.Context, move);
            var ppUps = pkm.GetPPUps()[moveIndex];

            return moveBasePP + moveBasePP * ppUps / 5;
        }

        /// <summary>
        /// Gets a specific relearn move by index.
        /// Relearn moves are available in Gen 6+ and represent moves a Pokémon can relearn.
        /// For pre-Gen 6 Pokemon, the properties exist but will typically be 0.
        /// </summary>
        /// <param name="index">The relearn move slot index (0-3).</param>
        /// <returns>The move ID, or 0 if the index is invalid.</returns>
        public ushort GetRelearnMove(int index)
        {
            if (index is < 0 or > 3)
            {
                return 0;
            }

            return index switch
            {
                0 => pkm.RelearnMove1,
                1 => pkm.RelearnMove2,
                2 => pkm.RelearnMove3,
                3 => pkm.RelearnMove4,
                _ => 0
            };
        }

        /// <summary>
        /// Sets a specific relearn move by index.
        /// Relearn moves are available in Gen 6+ and represent moves a Pokémon can relearn.
        /// </summary>
        /// <param name="index">The relearn move slot index (0-3).</param>
        /// <param name="move">The move ID to set.</param>
        public void SetRelearnMove(int index, ushort move)
        {
            if (index is < 0 or > 3)
            {
                return;
            }

            switch (index)
            {
                case 0:
                    pkm.RelearnMove1 = move;
                    break;
                case 1:
                    pkm.RelearnMove2 = move;
                    break;
                case 2:
                    pkm.RelearnMove3 = move;
                    break;
                case 3:
                    pkm.RelearnMove4 = move;
                    break;
            }
        }

        /// <summary>
        /// Gets all relearn moves as a read-only collection.
        /// </summary>
        /// <returns>A read-only collection of the four relearn move IDs.</returns>
        public ReadOnlyCollection<ushort> GetRelearnMoves() => new(
        [
            pkm.RelearnMove1,
            pkm.RelearnMove2,
            pkm.RelearnMove3,
            pkm.RelearnMove4
        ]);

        /// <summary>
        /// Determines if the Pokémon supports relearn moves (Gen 6+).
        /// </summary>
        /// <returns>True if the Pokémon has relearn move properties; otherwise, false.</returns>
        public bool HasRelearnMoves() => pkm.Format >= 6;

        /// <summary>
        /// Safely determines if the Pokémon is shiny, handling both Gen 1/2 (DV-based) and Gen 3+ (PID-based) shininess.
        /// In Gen 1/2, shininess is determined by specific DV (Determinant Value) patterns.
        /// In Gen 3+, shininess is determined by the PID (Personality ID) and trainer IDs.
        /// </summary>
        /// <returns>True if the Pokémon is shiny; otherwise, false.</returns>
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

            // Gen 1/2 shiny check: DVs must match the pattern 0x2AAA
            // This corresponds to specific stat DVs that result in shininess
            static bool GetIsShinyGb(ushort dv16) => (dv16 & 0x2FFF) == 0x2AAA;
        }

        /// <summary>
        /// Safely sets the shininess of the Pokémon, handling both Gen 1/2 and Gen 3+ mechanics.
        /// For Gen 3+, uses PKHeX.Core's SetIsShiny method.
        /// For Gen 1/2, randomizes IVs until the desired shininess is achieved.
        /// </summary>
        /// <param name="shiny">True to make the Pokémon shiny; false to make it non-shiny.</param>
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

            // For Gen 1/2: Keep randomizing IVs until the Pokémon is no longer shiny
            do
            {
                pkm.SetRandomIVs();
            } while (pkm.GetIsShinySafe());
        }
    }
}
