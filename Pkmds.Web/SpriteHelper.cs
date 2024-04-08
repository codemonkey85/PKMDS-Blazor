namespace Pkmds.Web;

public static class SpriteHelper
{
    public static string GetPokemonSpriteFilename(PKM? pokemon) =>
        new StringBuilder("sprites/a/a_")
        .Append(pokemon switch
        {
            null => "unknown",
            { Species: (ushort)Species.Manaphy, IsEgg: true } => "490-e",
            { IsEgg: true } => "egg",
            { Species: (ushort)Species.Alcremie } => $"{pokemon.Species}-{pokemon.Form}-{pokemon.GetFormArgument(0)}",
            { Form: > 0 } => pokemon.Species switch
            {
                (ushort)Species.Scatterbug or (ushort)Species.Spewpa => pokemon.Species.ToString(),
                (ushort)Species.Urshifu => pokemon.Species.ToString(),
                _ => $"{pokemon.Species}-{pokemon.Form}",
            },
            { Species: > (ushort)Species.None and < (ushort)Species.MAX_COUNT } =>
                pokemon.Species.ToString(),
            _ => "unknown",
        })
        .Append(".png")
        .ToString();

    public static string GetBallSpriteFilename(int ball) =>
        $"sprites/b/_ball{ball}.png";

    public static string GetBigItemSpriteFilename(int item) =>
        $"sprites/bi/bitem_{item}.png";

    public static string GetArtworkItemSpriteFilename(int item) =>
        $"sprites/ai/aitem_{item}.png";

    public static string GetTypeGemSpriteFileName(byte type) =>
        $"sprites/t/g/gem_{type:00}.png";

    public static string GetTypeSquareSpriteFileName(byte type) =>
        $"sprites/t/s/type_icon_{type:00}.png";

    public static string GetTypeWideSpriteFileName(byte type) =>
        $"sprites/t/w/type_wide_{type:00}.png";

    public static string GetSpriteCssClass(PKM? pkm) =>
        $"d-flex align-items-center justify-center {(pkm is { Species: > 0 } ? "slot-fill" : string.Empty)}";
}
