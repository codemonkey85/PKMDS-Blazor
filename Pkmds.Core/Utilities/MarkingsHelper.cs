namespace Pkmds.Core.Utilities;

/// <summary>
/// Helper class for working with Pokémon markings.
/// Provides enum definitions and unicode symbols for the six marking shapes.
/// </summary>
public static class MarkingsHelper
{
    /// <summary>
    /// Enum representing the six types of Pokémon markings.
    /// Used for organizing and categorizing Pokémon in the PC.
    /// </summary>
    public enum Markings
    {
        /// <summary>Circle marking (●)</summary>
        Circle = 0,

        /// <summary>Triangle marking (▲)</summary>
        Triangle = 1,

        /// <summary>Square marking (■)</summary>
        Square = 2,

        /// <summary>Heart marking (♥)</summary>
        Heart = 3,

        /// <summary>Star marking (★)</summary>
        Star = 4,

        /// <summary>Diamond marking (♦)</summary>
        Diamond = 5
    }

    /// <summary>Unicode symbol for Circle marking (●)</summary>
    public const string Circle = "●";

    /// <summary>Unicode symbol for Triangle marking (▲)</summary>
    public const string Triangle = "▲";

    /// <summary>Unicode symbol for Square marking (■)</summary>
    public const string Square = "■";

    /// <summary>Unicode symbol for Heart marking (♥)</summary>
    public const string Heart = "♥";

    /// <summary>Unicode symbol for Star marking (★)</summary>
    public const string Star = "★";

    /// <summary>Unicode symbol for Diamond marking (♦)</summary>
    public const string Diamond = "♦";
}
