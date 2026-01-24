namespace Pkmds.Core.Utilities;

public static class GameInfoUtilities
{
    public static string GetCategoryName(int categoryId) =>
        categoryId switch
        {
            0 => "Status",
            1 => "Physical",
            2 => "Special",
            _ => "Unknown"
        };
}
