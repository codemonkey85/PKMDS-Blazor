namespace Pkmds.Blazor.Components;

public partial class BoxSlotComponent
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    private string GetPokemonSpriteCssClass()
    {
        if (Pokemon is null or { Species: 0 })
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        switch ((Species)Pokemon.Species)
        {
            case Species.NidoranF:
                sb.Append("nidoran-f");
                break;
            case Species.NidoranM:
                sb.Append("nidoran-m");
                break;
            case Species.Unown:
                sb.Append(Pokemon.Form switch
                {
                    00 => "unown-a",
                    01 => "unown-b",
                    02 => "unown-c",
                    03 => "unown-d",
                    04 => "unown-e",
                    05 => "unown-f",
                    06 => "unown-g",
                    07 => "unown-h",
                    08 => "unown-i",
                    09 => "unown-j",
                    10 => "unown-k",
                    11 => "unown-l",
                    12 => "unown-m",
                    13 => "unown-n",
                    14 => "unown-o",
                    15 => "unown-p",
                    16 => "unown-q",
                    17 => "unown-r",
                    18 => "unown-s",
                    19 => "unown-t",
                    20 => "unown-u",
                    21 => "unown-v",
                    22 => "unown-w",
                    23 => "unown-x",
                    24 => "unown-y",
                    25 => "unown-z",
                    26 => "unown-question",
                    27 => "unown-exclamation",
                    _ => "unown"
                });
                break;
            default:
                sb.Append(GetPokemonSpriteCssClassFromId(Pokemon.Species));
                break;
        }

        if ((Species)Pokemon.Species is
            Species.Rattata or Species.Raticate or
            Species.Raichu or
            Species.Sandshrew or Species.Sandslash or
            Species.Vulpix or Species.Ninetales or
            Species.Diglett or Species.Dugtrio or
            Species.Meowth or Species.Persian or
            Species.Geodude or Species.Graveler or Species.Golem or
            Species.Grimer or Species.Muk or
            Species.Exeggutor or
            Species.Marowak
            && Pokemon.Form == 1)
        {
            sb.Append("-alola");
        }

        if ((Species)Pokemon.Species is
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
            Species.Stunfisk
            && Pokemon.Form == 2)
        {
            sb.Append("-galar");
        }

        if ((Species)Pokemon.Species is
            Species.Tornadus or
            Species.Thundurus or
            Species.Landorus or
            Species.Enamorus)
        {
            sb.Append(Pokemon.Form == 0 ? "-incarnate" : "-therian");
        }

        if ((Species)Pokemon.Species is Species.Deoxys)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-normal",
                1 => "-attack",
                2 => "-defense",
                3 => "-speed",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Burmy or
            Species.Wormadam or
            Species.Mothim)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-plant",
                2 => "-sandy",
                3 => "-trash",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Shellos or
            Species.Gastrodon)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-west",
                2 => "-east",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Rotom)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-fan",
                2 => "-frost",
                3 => "-heat",
                4 => "-mow",
                5 => "-wash",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Dialga or
            Species.Palkia)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-origin",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Giratina)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-altered",
                1 => "-origin",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Shaymin)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-land",
                1 => "-sky",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Arceus or
            Species.Silvally)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-normal",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Basculin)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-red-striped",
                1 => "-blue-striped",
                2 => "-white-striped",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Deerling or
            Species.Sawsbuck)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-spring",
                1 => "-autumn",
                2 => "-summer",
                3 => "-winter",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Kyurem)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-black",
                2 => "-white",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Keldeo)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-ordinary",
                1 => "-resolute",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Meloetta)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-aria",
                1 => "-pirouette",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Genesect)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-standard",
                1 => "-burn",
                2 => "-chill",
                3 => "-douse",
                4 => "-shock",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Scatterbug or
            Species.Vivillon)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is
            Species.Flabébé or
            Species.Floette or
            Species.Florges)
        {
            sb.Append(Pokemon.Form switch
            {
                0 => "-red",
                1 => "-blue",
                2 => "-orange",
                3 => "-white",
                4 => "-yellow",
                5 => "-eternal",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Furfrou)
        {
            sb.Append(Pokemon.Form switch
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
            });
        }

        if ((Species)Pokemon.Species is
            Species.Pumpkaboo or
            Species.Gourgeist)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-average",
                2 => "-large",
                3 => "-small",
                4 => "-super",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Zygarde)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-10",
                2 => "-complete",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Oricorio)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-baile",
                2 => "-pau",
                3 => "-pom-pom",
                4 => "-sensu",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Lycanroc)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-midday",
                2 => "-midnight",
                3 => "-dusk",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Minior)
        {
            sb.Append(Pokemon.Form switch
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
            });
        }

        if ((Species)Pokemon.Species is Species.Necrozma)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-dawn",
                2 => "-dusk",
                3 => "-ultra",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Magearna)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-original",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Toxtricity)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-amped",
                2 => "-low-key",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Alcremie)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-caramel-swirl-berry",
                _ => string.Empty,
            });
        }

        if ((Species)Pokemon.Species is Species.Zarude)
        {
            sb.Append(Pokemon.Form switch
            {
                1 => "-dada",
                _ => string.Empty,
            });
        }

        if (Pokemon is { IsShiny: true })
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
