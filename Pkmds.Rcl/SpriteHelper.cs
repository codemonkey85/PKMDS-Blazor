namespace Pkmds.Rcl;

public static class SpriteHelper
{
    public static string GetPokemonSpriteCssClass(PKM? pokemon)
    {
        if (pokemon is null or { Species: (ushort)Species.None })
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.Append((Species)pokemon.Species switch
        {
            Species.NidoranF => "nidoran-f",
            Species.NidoranM => "nidoran-m",
            _ => GetPokemonSpriteCssClassFromId(pokemon.Species),
        });

        sb.Append((Species)pokemon.Species switch
        {
            Species.Rattata or Species.Raticate or
            Species.Raichu or
            Species.Sandshrew or Species.Sandslash or
            Species.Vulpix or Species.Ninetales or
            Species.Diglett or Species.Dugtrio or
            Species.Meowth or Species.Persian or
            Species.Geodude or Species.Graveler or Species.Golem or
            Species.Grimer or Species.Muk or
            Species.Exeggutor or
            Species.Marowak => pokemon.Form == 1 ? "-alola" : string.Empty,

            Species.Meowth or
            Species.Ponyta or Species.Rapidash or
            Species.Slowpoke or Species.Slowbro or Species.Slowking or
            Species.Farfetchd or
            Species.Weezing or
            Species.MrMime or
            Species.Corsola or
            Species.Zigzagoon or Species.Linoone or
            Species.Darumaka or Species.Darmanitan or
            Species.Yamask or
            Species.Stunfisk => pokemon.Form == 2 ? "-galar" : string.Empty,

            Species.Tornadus or
            Species.Thundurus or
            Species.Landorus or
            Species.Enamorus => pokemon.Form == 0 ? "-incarnate" : "-therian",

            Species.Unown => pokemon.Form switch
            {
                00 => "-a",
                01 => "-b",
                02 => "-c",
                03 => "-d",
                04 => "-e",
                05 => "-f",
                06 => "-g",
                07 => "-h",
                08 => "-i",
                09 => "-j",
                10 => "-k",
                11 => "-l",
                12 => "-m",
                13 => "-n",
                14 => "-o",
                15 => "-p",
                16 => "-q",
                17 => "-r",
                18 => "-s",
                19 => "-t",
                20 => "-u",
                21 => "-v",
                22 => "-w",
                23 => "-x",
                24 => "-y",
                25 => "-z",
                26 => "-question",
                27 => "-exclamation",
                _ => string.Empty
            },

            Species.Deoxys => pokemon.Form switch
            {
                0 => "-normal",
                1 => "-attack",
                2 => "-defense",
                3 => "-speed",
                _ => string.Empty,
            },

            Species.Burmy or
            Species.Wormadam or
            Species.Mothim => pokemon.Form switch
            {
                1 => "-plant",
                2 => "-sandy",
                3 => "-trash",
                _ => string.Empty,
            },

            Species.Shellos or
            Species.Gastrodon => pokemon.Form switch
            {
                1 => "-west",
                2 => "-east",
                _ => string.Empty,
            },

            Species.Rotom => pokemon.Form switch
            {
                1 => "-fan",
                2 => "-frost",
                3 => "-heat",
                4 => "-mow",
                5 => "-wash",
                _ => string.Empty,
            },

            Species.Dialga or
            Species.Palkia => pokemon.Form switch
            {
                1 => "-origin",
                _ => string.Empty,
            },

            Species.Giratina => pokemon.Form switch
            {
                0 => "-altered",
                1 => "-origin",
                _ => string.Empty,
            },

            Species.Shaymin => pokemon.Form switch
            {
                0 => "-land",
                1 => "-sky",
                _ => string.Empty,
            },

            Species.Arceus or
            Species.Silvally => pokemon.Form switch
            {
                0 => "-normal",
                _ => string.Empty,
            },

            Species.Basculin => pokemon.Form switch
            {
                0 => "-red-striped",
                1 => "-blue-striped",
                2 => "-white-striped",
                _ => string.Empty,
            },

            Species.Deerling or
            Species.Sawsbuck => pokemon.Form switch
            {
                0 => "-spring",
                1 => "-autumn",
                2 => "-summer",
                3 => "-winter",
                _ => string.Empty,
            },

            Species.Kyurem => pokemon.Form switch
            {
                1 => "-black",
                2 => "-white",
                _ => string.Empty,
            },

            Species.Keldeo => pokemon.Form switch
            {
                0 => "-ordinary",
                1 => "-resolute",
                _ => string.Empty,
            },

            Species.Meloetta => pokemon.Form switch
            {
                0 => "-aria",
                1 => "-pirouette",
                _ => string.Empty,
            },

            Species.Genesect => pokemon.Form switch
            {
                0 => "-standard",
                1 => "-burn",
                2 => "-chill",
                3 => "-douse",
                4 => "-shock",
                _ => string.Empty,
            },

            Species.Scatterbug or
            Species.Vivillon => pokemon.Form switch
            {
                0 => "",
                _ => string.Empty,
            },

            Species.Flabébé or
            Species.Floette or
            Species.Florges => pokemon.Form switch
            {
                0 => "-red",
                1 => "-blue",
                2 => "-orange",
                3 => "-white",
                4 => "-yellow",
                5 => "-eternal",
                _ => string.Empty,
            },

            Species.Furfrou => pokemon.Form switch
            {
                1 => "-natural",
                2 => "-dandy",
                3 => "-debutante",
                4 => "-diamond",
                5 => "-heart",
                6 => "-kabuki",
                7 => "-la-reine",
                8 => "-matron",
                9 => "-pharaoh",
                10 => "-star",
                _ => string.Empty,
            },

            Species.Pumpkaboo or
            Species.Gourgeist => pokemon.Form switch
            {
                1 => "-average",
                2 => "-large",
                3 => "-small",
                4 => "-super",
                _ => string.Empty,
            },

            Species.Zygarde => pokemon.Form switch
            {
                1 => "-10",
                2 => "-complete",
                _ => string.Empty,
            },

            Species.Oricorio => pokemon.Form switch
            {
                1 => "-baile",
                2 => "-pau",
                3 => "-pom-pom",
                4 => "-sensu",
                _ => string.Empty,
            },

            Species.Lycanroc => pokemon.Form switch
            {
                1 => "-midday",
                2 => "-midnight",
                3 => "-dusk",
                _ => string.Empty,
            },

            Species.Minior => pokemon.Form switch
            {
                1 => "-blue-meteor",
                2 => "-green-meteor",
                3 => "-indigo-meteor",
                4 => "-orange-meteor",
                5 => "-red-meteor",
                6 => "-violet-meteor",
                7 => "-yellow-meteor",
                8 => "-blue",
                9 => "-green",
                10 => "-indigo",
                11 => "-orange",
                12 => "-red",
                13 => "-violet",
                14 => "-yellow",
                _ => string.Empty,
            },

            Species.Necrozma => pokemon.Form switch
            {
                1 => "-dawn",
                2 => "-dusk",
                3 => "-ultra",
                _ => string.Empty,
            },

            Species.Magearna => pokemon.Form switch
            {
                1 => "-original",
                _ => string.Empty,
            },

            Species.Toxtricity => pokemon.Form switch
            {
                1 => "-amped",
                2 => "-low-key",
                _ => string.Empty,
            },

            Species.Alcremie => pokemon.Form switch
            {
                1 => "-caramel-swirl-berry",
                _ => string.Empty,
            },

            Species.Zarude => pokemon.Form switch
            {
                1 => "-dada",
                _ => string.Empty,
            },

            _ => string.Empty,
        });

        if (pokemon is { IsShiny: true })
        {
            sb.Append(" shiny");
        }

        return sb.ToString();
    }

    private static string GetPokemonSpriteCssClassFromId(ushort id) =>
        SpeciesName.GetSpeciesName(id, (int)LanguageID.English)
            .ToLower()
            .Replace(" ", string.Empty)
            .Replace("'", string.Empty)
            .Replace("é", "e")
            .Replace(".", "-");
}
