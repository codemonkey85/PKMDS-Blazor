namespace Pkmds.Rcl.Extensions;

public static class GameInfoExtensions
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
