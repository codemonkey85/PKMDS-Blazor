namespace Pkmds.Rcl;

public static class SpriteHelper
{
    public static string GetPokemonSpriteFilename(PKM? pokemon) =>
        new StringBuilder("_content/Pkmds.Rcl/sprites/a/a_")
        .Append(pokemon switch
        {
            null => "unknown",
            { Species: (ushort)Species.Manaphy, IsEgg: true } => "490-e",
            { IsEgg: true } => "egg",
            { Species: (ushort)Species.Alcremie } => $"{pokemon.Species}-{pokemon.Form}-{pokemon.GetFormArgument(0)}",
            { Form: > 0 } => pokemon.Species switch
            {
                (ushort)Species.Scatterbug or (ushort)Species.Spewpa => pokemon.Species.ToString(),
                _ => $"{pokemon.Species}-{pokemon.Form}",
            },
            { Species: > (ushort)Species.None and < (ushort)Species.MAX_COUNT } =>
                pokemon.Species.ToString(),
            _ => "unknown",
        })
        .Append(".png").ToString();

    public static string GetBallSpriteFilename(int ball) =>
        $"_content/Pkmds.Rcl/sprites/b/_ball{ball}.png";
}
