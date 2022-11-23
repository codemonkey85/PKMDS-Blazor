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
            Species.Persian or
            Species.Geodude or Species.Graveler or Species.Golem or
            Species.Grimer or Species.Muk or
            Species.Exeggutor or
            Species.Marowak => pokemon.Form == 1 ? "-alola" : string.Empty,

            Species.Ponyta or Species.Rapidash or
            Species.Slowpoke or Species.Slowbro or Species.Slowking or
            Species.Farfetchd or
            Species.Weezing or
            Species.MrMime or
            Species.Corsola or
            Species.Zigzagoon or Species.Linoone or
            Species.Darumaka or Species.Darmanitan or
            Species.Yamask or
            Species.Stunfisk => pokemon.Form == 1 ? "-galar" : string.Empty,

            Species.Meowth => pokemon.Form switch
            {
                1 => "-alola",
                2 => "-galar",
                _ => string.Empty,
            },

            Species.Tornadus or
            Species.Thundurus or
            Species.Landorus or
            Species.Enamorus => pokemon.Form == 0 ? "-incarnate" : "-therian",

            Species.Unown => pokemon.Form switch
            {
                0 => "-a",
                1 => "-b",
                2 => "-c",
                3 => "-d",
                4 => "-e",
                5 => "-f",
                6 => "-g",
                7 => "-h",
                8 => "-i",
                9 => "-j",
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
                0 => "-plant",
                1 => "-sandy",
                2 => "-trash",
                _ => string.Empty,
            },

            Species.Shellos or
            Species.Gastrodon => pokemon.Form switch
            {
                0 => "-west",
                1 => "-east",
                _ => string.Empty,
            },

            Species.Rotom => pokemon.Form switch
            {
                1 => "-heat",
                2 => "-wash",
                3 => "-frost",
                4 => "-fan",
                5 => "-mow",
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
                1 => "-fighting",
                2 => "-flying",
                3 => "-poison",
                4 => "-ground",
                5 => "-rock",
                6 => "-bug",
                7 => "-ghost",
                8 => "-steel",
                9 => "-fire",
                10 => "-water",
                11 => "-grass",
                12 => "-electric",
                13 => "-psychic",
                14 => "-ice",
                15 => "-dragon",
                16 => "-dark",
                17 => "-fairy",
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
                1 => "-summer",
                2 => "-autumn",
                3 => "-winter",
                _ => string.Empty,
            },

            Species.Kyurem => pokemon.Form switch
            {
                1 => "-white",
                2 => "-black",
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
                1 => "-douse",
                2 => "-shock",
                3 => "-burn",
                4 => "-chill",
                _ => string.Empty,
            },

            Species.Scatterbug or
            Species.Spewpa or
            Species.Vivillon => pokemon.Form switch
            {
                0 => "-icy-snow",
                1 => "-polar",
                2 => "-tundra",
                3 => "-continental",
                4 => "-garden",
                5 => "-elegant",
                6 => "-meadow",
                7 => "-modern",
                8 => "-marine",
                9 => "-archipelago",
                10 => "-high-plains",
                11 => "-sandstorm",
                12 => "-river",
                13 => "-monsoon",
                14 => "-savanna",
                15 => "-sun",
                16 => "-ocean",
                17 => "-jungle",
                18 => "-fancy",
                19 => "-poke-ball",
                _ => string.Empty,
            },

            Species.Flabébé or
            Species.Floette or
            Species.Florges => pokemon.Form switch
            {
                0 => "-red",
                1 => "-yellow",
                2 => "-orange",
                3 => "-blue",
                4 => "-white",
                5 => "-eternal",
                _ => string.Empty,
            },

            Species.Furfrou => pokemon.Form switch
            {
                0 => "-natural",
                1 => "-heart",
                2 => "-star",
                3 => "-diamond",
                4 => "-debutante",
                5 => "-matron",
                6 => "-dandy",
                7 => "-la-reine",
                8 => "-kabuki",
                9 => "-pharaoh",
                _ => string.Empty,
            },

            Species.Pumpkaboo or
            Species.Gourgeist => pokemon.Form switch
            {
                0 => "-average",
                1 => "-small",
                2 => "-large",
                3 => "-super",
                _ => string.Empty,
            },

            Species.Zygarde => pokemon.Form switch
            {
                1 => "-10",
                2 => "-10",
                3 => "-50",
                4 => "-complete",
                _ => string.Empty,
            },

            Species.Oricorio => pokemon.Form switch
            {
                0 => "-baile",
                1 => "-pom-pom",
                2 => "-pau",
                3 => "-sensu",
                _ => string.Empty,
            },

            Species.Lycanroc => pokemon.Form switch
            {
                0 => "-midday",
                1 => "-midnight",
                2 => "-dusk",
                _ => string.Empty,
            },

            Species.Minior => pokemon.Form switch
            {
                0 => "-red-meteor",
                1 => "-orange-meteor",
                2 => "-yellow-meteor",
                3 => "-green-meteor",
                4 => "-blue-meteor",
                5 => "-indigo-meteor",
                6 => "-violet-meteor",
                7 => "-red",
                8 => "-orange",
                9 => "-yellow",
                10 => "-green",
                11 => "-blue",
                12 => "-indigo",
                13 => "-violet",
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
                0 => "-amped",
                1 => "-low-key",
                _ => string.Empty,
            },

            Species.Alcremie => GetAlcremieForm(pokemon),

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

        if (pokemon.Gender == (int)Gender.Female && (Species)pokemon.Species is
            Species.Pikachu or
            Species.Hippopotas or
            Species.Hippowdon or
            Species.Unfezant or
            Species.Frillish or
            Species.Jellicent or
            Species.Pyroar or
            Species.Meowstic or
            Species.Indeedee)
        {
            sb.Append(" female");
        }

        return sb.ToString();

        static string GetPokemonSpriteCssClassFromId(ushort id)
        {
            var name = SpeciesName.GetSpeciesName(id, (int)LanguageID.English)
            .ToLower()
            .Replace(":", string.Empty)
            .Replace("'", string.Empty)
            .Replace("’", string.Empty)
            .Replace("♀", string.Empty)
            .Replace("♂", string.Empty)
            .Replace(" ", "-")
            .Replace("é", "e")
            .Replace(".", "-")
            .Replace("--", "-");
            return name.Last() == '-' ? name.Remove(name.Length - 1) : name;
        }

        static string GetAlcremieForm(PKM pokemon)
        {
            var flavor = pokemon.Form;
            var sbForm = new StringBuilder();
            sbForm.Append(flavor switch
            {
                0 => "-vanilla-cream",
                1 => "-ruby-cream",
                2 => "-matcha-cream",
                3 => "-mint-cream",
                4 => "-lemon-cream",
                5 => "-salted-cream",
                6 => "-ruby-swirl",
                7 => "-caramel-swirl",
                8 => "-rainbow-swirl",
                _ => string.Empty,
            });

            if (sbForm.Length == 0)
            {
                return string.Empty;
            }

            var decoration = pokemon.GetFormArgument();
            sbForm.Append(decoration switch
            {
                null => "-plain",
                0 => "-strawberry",
                1 => "-berry",
                2 => "-love",
                3 => "-star",
                4 => "-clover",
                5 => "-flower",
                6 => "-ribbon",
                _ => "-plain",
            });

            return sbForm.ToString();
        }
    }
}
