namespace Pkmds.Rcl;

/// <summary>
/// Application-wide constants used throughout the PKMDS application.
/// </summary>
public static class Constants
{
    /// <summary>Full application title displayed in the UI.</summary>
    public const string AppTitle = "PKMDS: Pokémon Save Editor for Web";

    /// <summary>Shortened application title for compact displays.</summary>
    public const string AppShortTitle = "PKMDS for Web";

    /// <summary>CSS class applied to selected Pokémon slots for highlighting.</summary>
    public const string SelectedSlotClass = "slot-selected";

    /// <summary>Maximum file size allowed for uploads (64 MB).</summary>
    public const long MaxFileSize = 67_108_864L; // 64 bytes in binary

    /// <summary>Unicode symbol for male gender (♂).</summary>
    public const string MaleGenderUnicode = "♂";

    /// <summary>Unicode symbol for female gender (♀).</summary>
    public const string FemaleGenderUnicode = "♀";

    /// <summary>SVG path data for the Pokédollar currency symbol.</summary>
    public const string PokeDollarSvg = """
                                        <svg xmlns="http://www.w3.org/2000/svg"
                                             viewBox="0 0 1366 2048">
                                             <path d="M6 1389v-172h152v-144H6V901h152V70h710q171 0 290.5 97.5T1278 414t-119.5 246.5T868 758H352v143h662v172H352v144h662v172H352v147H158v-147H6zm346-803h516q41 0 90-17 35-13 70-41 55-50 55-114 0-66-55-115-35-28-70-41-46-16-90-16H352v344z"/>
                                        </svg>
                                        """;

    /// <summary>Placeholder text for empty dropdown selections.</summary>
    public const string EmptyIndex = "---";

    /// <summary>Maximum Dynamax level (Gen 8 mechanic).</summary>
    public const byte MaxDynamaxLevel = 10;

    /// <summary>Minimum value for minutes in time inputs.</summary>
    public const byte MinMinutes = 0;

    /// <summary>Maximum value for minutes in time inputs.</summary>
    public const byte MaxMinutes = 59;

    /// <summary>Minimum value for seconds in time inputs.</summary>
    public const byte MinSeconds = 0;

    /// <summary>Maximum value for seconds in time inputs.</summary>
    public const byte MaxSeconds = 59;

    /// <summary>Input mask pattern for hexadecimal values.</summary>
    public static readonly PatternMask HexMask = new("########") { MaskChars = [new('#', "[0-9a-fA-F]")] };
}
