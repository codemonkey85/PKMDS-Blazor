namespace Pkmds.Rcl.Extensions;

public static class MoveExtension
{
    public static bool IsValidMove(this Move move) => move is > Move.None and < Move.MAX_COUNT;
}
