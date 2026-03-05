namespace Pkmds.Core.Utilities;

/// <summary>
/// Utility methods for working with PKHeX GameInfo data.
/// </summary>
public static class GameInfoUtilities
{
    /// <summary>
    /// Gets the localized name of a move category (Status, Physical, or Special).
    /// </summary>
    /// <param name="categoryId">The category ID (0 = Status, 1 = Physical, 2 = Special).</param>
    /// <returns>The category name, or "Unknown" if the ID is not recognized.</returns>
    public static string GetCategoryName(int categoryId) =>
        categoryId switch
        {
            0 => "Status",
            1 => "Physical",
            2 => "Special",
            _ => "Unknown"
        };
}
