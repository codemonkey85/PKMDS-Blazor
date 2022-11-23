namespace Pkmds.Rcl.Extensions;

public static class PKMExtensions
{
    public static uint? GetFormArgument(this PKM pkm, uint? valueIfNull = null) =>
        (pkm as IFormArgument)?.FormArgument ?? valueIfNull;
}
