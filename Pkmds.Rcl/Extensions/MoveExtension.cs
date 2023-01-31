namespace Pkmds.Rcl.Extensions;

public static class MoveExtension
{
    public static bool IsValidMove(this Move move) => move is not Move.None or Move.MAX_COUNT;
}
