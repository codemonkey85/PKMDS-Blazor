namespace Pkmds.Web.Extensions;

public static class MoveExtension
{
    public static bool IsValidMove(this Move move) => move is > Move.None and < Move.MAX_COUNT;

    public static bool IsValidMove(this ushort move) => IsValidMove((Move)move);
}
