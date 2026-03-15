using static Pkmds.Core.Utilities.GameInfoUtilities;

namespace Pkmds.Rcl;

/// <summary>
/// Helper class for generating file paths to Pokémon and item sprite images.
/// Handles sprite selection based on species, form, gender, context, and other attributes.
/// </summary>
public static partial class ImageHelper
{
    private const string SpritesRoot = "_content/Pkmds.Rcl/sprites/";
    private const string PokeApiHomeBaseUrl =
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/home/";
    private const string PokeApiVersionsBaseUrl =
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/";
    private const int PikachuStarterForm = 8;
    private const int EeveeStarterForm = 1;

    // Alcremie (869): 9 cream forms × 7 sweet decorations = 63 combinations.
    // PKHeX form (0-8) = cream type; GetFormArgument(0) (0-6) = sweet type.
    private static readonly string[] AlcremieCreamNames =
    [
        "vanilla-cream", "ruby-cream", "matcha-cream", "mint-cream",
        "lemon-cream", "salted-cream", "ruby-swirl", "caramel-swirl", "rainbow-swirl",
    ];

    private static readonly string[] AlcremieSweetNames =
    [
        "strawberry-sweet", "berry-sweet", "love-sweet", "star-sweet",
        "clover-sweet", "flower-sweet", "ribbon-sweet",
    ];

    // Maps (species, form) → PokeAPI form-name suffix (kebab-case, no leading dash).
    // Only includes forms whose home sprite is a named file (e.g. 201-b.png, 666-icy-snow.png).
    // Regional variants and other forms using 10xxx PokeAPI IDs are in PokeApiFormIds below.
    private static readonly Dictionary<(ushort Species, byte Form), string> PokeApiFormSuffixes = new()
    {
        // Unown (201) — forms 0-25 = a-z, 26 = exclamation, 27 = question.
        // Form 0 (A) uses 201.png for HOME (both 201.png and 201-a.png exist); game sprite dirs only have 201-a.png.
        { (201, 0), "a" },
        { (201, 1), "b" }, { (201, 2), "c" }, { (201, 3), "d" }, { (201, 4), "e" },
        { (201, 5), "f" }, { (201, 6), "g" }, { (201, 7), "h" }, { (201, 8), "i" },
        { (201, 9), "j" }, { (201, 10), "k" }, { (201, 11), "l" }, { (201, 12), "m" },
        { (201, 13), "n" }, { (201, 14), "o" }, { (201, 15), "p" }, { (201, 16), "q" },
        { (201, 17), "r" }, { (201, 18), "s" }, { (201, 19), "t" }, { (201, 20), "u" },
        { (201, 21), "v" }, { (201, 22), "w" }, { (201, 23), "x" }, { (201, 24), "y" },
        { (201, 25), "z" }, { (201, 26), "exclamation" }, { (201, 27), "question" },
        // Burmy (412) — sandy, trash; form 0 (plant) uses base URL (412.png)
        { (412, 1), "sandy" }, { (412, 2), "trash" },
        // NOTE: Wormadam (413) home sprites are 10xxx IDs, not named-suffix files — see PokeApiFormIds.
        // Cherrim (421) — form 1 = sunshine; form 0 (overcast) uses base URL
        { (421, 1), "sunshine" },
        // Shellos (422) / Gastrodon (423) — form 1 = east; form 0 (west) uses base URL
        { (422, 1), "east" },
        { (423, 1), "east" },
        // Arceus (493) — all non-Normal types; form 0 (Normal) uses base URL
        { (493, 1), "fighting" }, { (493, 2), "flying" }, { (493, 3), "poison" },
        { (493, 4), "ground" }, { (493, 5), "rock" }, { (493, 6), "bug" },
        { (493, 7), "ghost" }, { (493, 8), "steel" }, { (493, 9), "fire" },
        { (493, 10), "water" }, { (493, 11), "grass" }, { (493, 12), "electric" },
        { (493, 13), "psychic" }, { (493, 14), "ice" }, { (493, 15), "dragon" },
        { (493, 16), "dark" }, { (493, 17), "fairy" },
        // Deerling (585) / Sawsbuck (586) — summer, autumn, winter; form 0 (spring) uses base URL
        { (585, 1), "summer" }, { (585, 2), "autumn" }, { (585, 3), "winter" },
        { (586, 1), "summer" }, { (586, 2), "autumn" }, { (586, 3), "winter" },
        // Genesect (649) — burn, chill, douse, shock; form 0 (no drive) uses base URL
        { (649, 1), "burn" }, { (649, 2), "chill" }, { (649, 3), "douse" }, { (649, 4), "shock" },
        // Vivillon (666) — all 20 patterns are named; no plain base URL exists
        { (666, 0), "icy-snow" }, { (666, 1), "polar" }, { (666, 2), "tundra" },
        { (666, 3), "continental" }, { (666, 4), "garden" }, { (666, 5), "elegant" },
        { (666, 6), "meadow" }, { (666, 7), "modern" }, { (666, 8), "marine" },
        { (666, 9), "archipelago" }, { (666, 10), "high-plains" }, { (666, 11), "sandstorm" },
        { (666, 12), "river" }, { (666, 13), "monsoon" }, { (666, 14), "savanna" },
        { (666, 15), "sun" }, { (666, 16), "ocean" }, { (666, 17), "jungle" },
        { (666, 18), "fancy" }, { (666, 19), "poke-ball" },
        // Flabébé (669) / Floette (670) / Florges (671) — all 5 flower colors are named
        // PKHeX form order: 0=Red, 1=Yellow, 2=Orange, 3=Blue, 4=White
        { (669, 0), "red" }, { (669, 1), "yellow" }, { (669, 2), "orange" }, { (669, 3), "blue" }, { (669, 4), "white" },
        { (670, 0), "red" }, { (670, 1), "yellow" }, { (670, 2), "orange" }, { (670, 3), "blue" }, { (670, 4), "white" },
        { (671, 0), "red" }, { (671, 1), "yellow" }, { (671, 2), "orange" }, { (671, 3), "blue" }, { (671, 4), "white" },
        // Furfrou (676) — 9 trim styles; form 0 (natural) uses base URL
        { (676, 1), "heart" }, { (676, 2), "star" }, { (676, 3), "diamond" },
        { (676, 4), "debutante" }, { (676, 5), "matron" }, { (676, 6), "dandy" },
        { (676, 7), "la-reine" }, { (676, 8), "kabuki" }, { (676, 9), "pharaoh" },
        // Xerneas (716) — form 1 = neutral; form 0 (active) uses base URL
        { (716, 1), "neutral" },
        // Silvally (773) — all non-Normal types; form 0 (Normal) uses base URL
        { (773, 1), "fighting" }, { (773, 2), "flying" }, { (773, 3), "poison" },
        { (773, 4), "ground" }, { (773, 5), "rock" }, { (773, 6), "bug" },
        { (773, 7), "ghost" }, { (773, 8), "steel" }, { (773, 9), "fire" },
        { (773, 10), "water" }, { (773, 11), "grass" }, { (773, 12), "electric" },
        { (773, 13), "psychic" }, { (773, 14), "ice" }, { (773, 15), "dragon" },
        { (773, 16), "dark" }, { (773, 17), "fairy" },
    };

    // Maps PKHeX (species, form) → PokeAPI pokemon ID for forms stored as numeric-ID sprite files.
    // Covers Mega/Primal, regional variants (Alolan/Galarian/Hisuian/Paldean), battle-only forms,
    // Wormadam (uses 10xxx IDs, not named suffixes), gender-as-form species, and Gen 6-9 alternates.
    // IDs verified against ~/Code/sprites/sprites/pokemon/other/home/ and PokeAPI CSV data.
    private static readonly Dictionary<(ushort Species, byte Form), uint> PokeApiFormIds = new()
    {
        // ── Mega / Primal forms (Gen 6-7) ──────────────────────────────────────
        { (3,   1), 10033 }, // Venusaur-Mega
        { (6,   1), 10034 }, { (6,   2), 10035 }, // Charizard-Mega-X / -Y
        { (9,   1), 10036 }, // Blastoise-Mega
        { (15,  1), 10090 }, // Beedrill-Mega
        { (18,  1), 10073 }, // Pidgeot-Mega
        { (65,  1), 10037 }, // Alakazam-Mega
        { (80,  1), 10071 }, // Slowbro-Mega         (PKHeX: 0=base, 1=Mega, 2=Galar)
        { (94,  1), 10038 }, // Gengar-Mega
        { (115, 1), 10039 }, // Kangaskhan-Mega
        { (127, 1), 10040 }, // Pinsir-Mega
        { (130, 1), 10041 }, // Gyarados-Mega
        { (142, 1), 10042 }, // Aerodactyl-Mega
        { (150, 1), 10043 }, { (150, 2), 10044 }, // Mewtwo-Mega-X / -Y
        { (181, 1), 10045 }, // Ampharos-Mega
        { (208, 1), 10072 }, // Steelix-Mega
        { (212, 1), 10046 }, // Scizor-Mega
        { (214, 1), 10047 }, // Heracross-Mega
        { (229, 1), 10048 }, // Houndoom-Mega
        { (248, 1), 10049 }, // Tyranitar-Mega
        { (254, 1), 10065 }, // Sceptile-Mega
        { (257, 1), 10050 }, // Blaziken-Mega
        { (260, 1), 10064 }, // Swampert-Mega
        { (282, 1), 10051 }, // Gardevoir-Mega
        { (302, 1), 10066 }, // Sableye-Mega
        { (303, 1), 10052 }, // Mawile-Mega
        { (306, 1), 10053 }, // Aggron-Mega
        { (308, 1), 10054 }, // Medicham-Mega
        { (310, 1), 10055 }, // Manectric-Mega
        { (319, 1), 10070 }, // Sharpedo-Mega
        { (323, 1), 10087 }, // Camerupt-Mega
        { (334, 1), 10067 }, // Altaria-Mega
        { (354, 1), 10056 }, // Banette-Mega
        { (359, 1), 10057 }, // Absol-Mega
        { (362, 1), 10074 }, // Glalie-Mega
        { (373, 1), 10089 }, // Salamence-Mega
        { (376, 1), 10076 }, // Metagross-Mega
        { (380, 1), 10062 }, // Latias-Mega
        { (381, 1), 10063 }, // Latios-Mega
        { (382, 1), 10077 }, // Kyogre-Primal
        { (383, 1), 10078 }, // Groudon-Primal
        { (384, 1), 10079 }, // Rayquaza-Mega
        { (428, 1), 10088 }, // Lopunny-Mega
        { (445, 1), 10058 }, // Garchomp-Mega
        { (448, 1), 10059 }, // Lucario-Mega
        { (460, 1), 10060 }, // Abomasnow-Mega
        { (475, 1), 10068 }, // Gallade-Mega
        { (531, 1), 10069 }, // Audino-Mega
        { (719, 1), 10075 }, // Diancie-Mega
        { (720, 1), 10086 }, // Hoopa-Unbound

        // ── Gen 3 alternate forms ─────────────────────────────────────────────
        { (386, 1), 10001 }, { (386, 2), 10002 }, { (386, 3), 10003 }, // Deoxys Attack/Defense/Speed

        // ── Gen 4 alternate forms ─────────────────────────────────────────────
        { (413, 1), 10004 }, { (413, 2), 10005 }, // Wormadam-Sandy / -Trash (plant = base 413.png)
        { (479, 1), 10008 }, { (479, 2), 10009 }, { (479, 3), 10010 }, // Rotom-Heat / -Wash / -Frost
        { (479, 4), 10011 }, { (479, 5), 10012 }, //                     -Fan / -Mow
        { (483, 1), 10245 }, // Dialga-Origin
        { (484, 1), 10246 }, // Palkia-Origin
        { (487, 1), 10007 }, // Giratina-Origin     (PKHeX: 0=Altered, 1=Origin)
        { (492, 1), 10006 }, // Shaymin-Sky         (PKHeX: 0=Land, 1=Sky)

        // ── Gen 5 alternate forms ─────────────────────────────────────────────
        { (351, 1), 10013 }, { (351, 2), 10014 }, { (351, 3), 10015 }, // Castform Sunny/Rainy/Snowy
        { (550, 1), 10016 }, { (550, 2), 10247 }, // Basculin Blue-Striped / White-Striped
        { (555, 1), 10017 }, // Darmanitan-Zen (Unova)  (PKHeX: 0=Std, 1=Zen, 2=Galar-Std, 3=Galar-Zen)
        { (641, 1), 10019 }, // Tornadus-Therian
        { (642, 1), 10020 }, // Thundurus-Therian
        { (645, 1), 10021 }, // Landorus-Therian
        { (646, 1), 10023 }, { (646, 2), 10022 }, // Kyurem-White / -Black
        { (647, 1), 10024 }, // Keldeo-Resolute
        { (648, 1), 10018 }, // Meloetta-Pirouette

        // ── Gen 6 alternate forms ─────────────────────────────────────────────
        { (658, 1), 10116 }, { (658, 2), 10117 }, // Greninja-Battle-Bond / -Ash
        { (670, 5), 10061 }, // Floette-Eternal — sprite not yet on PokeAPI CDN; pre-mapped to resolve automatically when added
        { (681, 1), 10026 }, // Aegislash-Blade      (PKHeX: 0=Shield, 1=Blade)
        { (710, 1), 10027 }, { (710, 2), 10028 }, { (710, 3), 10029 }, // Pumpkaboo Small/Large/Super
        { (711, 1), 10030 }, { (711, 2), 10031 }, { (711, 3), 10032 }, // Gourgeist Small/Large/Super
        { (718, 1), 10181 }, { (718, 2), 10118 }, { (718, 3), 10119 }, { (718, 4), 10120 }, // Zygarde 10%/10%-PC/50%-PC/Complete

        // ── Gen 7 alternate forms ─────────────────────────────────────────────
        { (25,  1), 10094 }, { (25,  2), 10095 }, { (25,  3), 10096 }, // Pikachu Original/Hoenn/Sinnoh cap
        { (25,  4), 10097 }, { (25,  5), 10098 }, { (25,  6), 10099 }, // Pikachu Unova/Kalos/Alola cap
        { (25,  7), 10148 }, { (25,  9), 10160 }, // Pikachu Partner cap / World cap
        { (741, 1), 10123 }, { (741, 2), 10124 }, { (741, 3), 10125 }, // Oricorio Pom-Pom/Pa'u/Sensu
        { (746, 1), 10127 }, // Wishiwashi-School
        { (774, 1), 10130 }, { (774, 2), 10131 }, { (774, 3), 10132 }, // Minior meteor Orange/Yellow/Green
        { (774, 4), 10133 }, { (774, 5), 10134 }, { (774, 6), 10135 }, // Minior meteor Blue/Indigo/Violet
        { (774, 7), 10136 }, { (774, 8), 10137 }, { (774, 9), 10138 }, // Minior core Red/Orange/Yellow
        { (774, 10), 10139 }, { (774, 11), 10140 }, { (774, 12), 10141 }, { (774, 13), 10142 }, // Minior core Green/Blue/Indigo/Violet
        { (778, 1), 10143 }, // Mimikyu-Busted
        { (801, 1), 10147 }, // Magearna-Original

        // ── Gen 8 alternate forms ─────────────────────────────────────────────
        { (845, 1), 10182 }, { (845, 2), 10183 }, // Cramorant-Gulping / -Gorging
        { (890, 1), 10190 }, // Eternatus-Eternamax (battle-only form, but sprite exists on CDN)
        { (875, 1), 10185 }, // Eiscue-Noice
        { (877, 1), 10187 }, // Morpeko-Hangry
        { (888, 1), 10188 }, // Zacian-Crowned
        { (889, 1), 10189 }, // Zamazenta-Crowned
        { (892, 1), 10191 }, // Urshifu-Rapid-Strike (PKHeX: 0=Single-Strike, 1=Rapid-Strike)
        { (893, 1), 10192 }, // Zarude-Dada
        { (898, 1), 10193 }, { (898, 2), 10194 }, // Calyrex-Ice-Rider / -Shadow-Rider

        // ── Alolan forms ──────────────────────────────────────────────────────
        { (19,  1), 10091 }, // Rattata-Alola
        { (20,  1), 10092 }, // Raticate-Alola
        { (26,  1), 10100 }, // Raichu-Alola
        { (27,  1), 10101 }, // Sandshrew-Alola
        { (28,  1), 10102 }, // Sandslash-Alola
        { (37,  1), 10103 }, // Vulpix-Alola
        { (38,  1), 10104 }, // Ninetales-Alola
        { (50,  1), 10105 }, // Diglett-Alola
        { (51,  1), 10106 }, // Dugtrio-Alola
        { (52,  1), 10107 }, // Meowth-Alola         (PKHeX: 0=Kanto, 1=Alolan, 2=Galarian)
        { (53,  1), 10108 }, // Persian-Alola
        { (74,  1), 10109 }, // Geodude-Alola
        { (75,  1), 10110 }, // Graveler-Alola
        { (76,  1), 10111 }, // Golem-Alola
        { (88,  1), 10112 }, // Grimer-Alola
        { (89,  1), 10113 }, // Muk-Alola
        { (103, 1), 10114 }, // Exeggutor-Alola
        { (105, 1), 10115 }, // Marowak-Alola

        // ── Galarian forms ────────────────────────────────────────────────────
        { (52,  2), 10161 }, // Meowth-Galar
        { (77,  1), 10162 }, // Ponyta-Galar
        { (78,  1), 10163 }, // Rapidash-Galar
        { (79,  1), 10164 }, // Slowpoke-Galar
        { (80,  2), 10165 }, // Slowbro-Galar         (PKHeX: 0=base, 1=Mega, 2=Galar)
        { (83,  1), 10166 }, // Farfetchd-Galar
        { (110, 1), 10167 }, // Weezing-Galar
        { (122, 1), 10168 }, // MrMime-Galar
        { (144, 1), 10169 }, // Articuno-Galar
        { (145, 1), 10170 }, // Zapdos-Galar
        { (146, 1), 10171 }, // Moltres-Galar
        { (199, 1), 10172 }, // Slowking-Galar
        { (222, 1), 10173 }, // Corsola-Galar
        { (263, 1), 10174 }, // Zigzagoon-Galar
        { (264, 1), 10175 }, // Linoone-Galar
        { (554, 1), 10176 }, // Darumaka-Galar
        { (555, 2), 10177 }, // Darmanitan-Galar-Standard
        { (555, 3), 10178 }, // Darmanitan-Galar-Zen
        { (562, 1), 10179 }, // Yamask-Galar
        { (618, 1), 10180 }, // Stunfisk-Galar

        // ── Hisuian forms ─────────────────────────────────────────────────────
        { (58,  1), 10229 }, // Growlithe-Hisui
        { (59,  1), 10230 }, // Arcanine-Hisui
        { (100, 1), 10231 }, // Voltorb-Hisui
        { (101, 1), 10232 }, // Electrode-Hisui
        { (157, 1), 10233 }, // Typhlosion-Hisui
        { (211, 1), 10234 }, // Qwilfish-Hisui
        { (215, 1), 10235 }, // Sneasel-Hisui
        { (503, 1), 10236 }, // Samurott-Hisui
        { (549, 1), 10237 }, // Lilligant-Hisui
        { (570, 1), 10238 }, // Zorua-Hisui
        { (571, 1), 10239 }, // Zoroark-Hisui
        { (628, 1), 10240 }, // Braviary-Hisui
        { (705, 1), 10241 }, // Sliggoo-Hisui
        { (706, 1), 10242 }, // Goodra-Hisui
        { (713, 1), 10243 }, // Avalugg-Hisui
        { (724, 1), 10244 }, // Decidueye-Hisui

        // ── Paldean forms ─────────────────────────────────────────────────────
        { (128, 1), 10250 }, // Tauros-Paldea-Combat (PKHeX: 0=base, 1=Combat, 2=Blaze, 3=Aqua)
        { (128, 2), 10251 }, // Tauros-Paldea-Blaze
        { (128, 3), 10252 }, // Tauros-Paldea-Aqua
        { (194, 1), 10253 }, // Wooper-Paldea

        // ── Other Gen 7–8 alternates ──────────────────────────────────────────
        { (745, 1), 10126 }, // Lycanroc-Midnight    (PKHeX: 0=Midday, 1=Midnight, 2=Dusk)
        { (745, 2), 10152 }, // Lycanroc-Dusk
        { (800, 1), 10155 }, // Necrozma-Dusk-Mane   (PKHeX: 0=base, 1=Dusk-Mane, 2=Dawn-Wings, 3=Ultra)
        { (800, 2), 10156 }, // Necrozma-Dawn-Wings
        { (800, 3), 10157 }, // Necrozma-Ultra
        { (849, 1), 10184 }, // Toxtricity-Low-Key   (PKHeX: 0=Amped, 1=Low-Key)

        // ── Gender-as-form species (female form → female/{id}.png) ────────────
        { (678, 1), 10025 }, // Meowstic-Female
        { (876, 1), 10186 }, // Indeedee-Female
        { (902, 1), 10248 }, // Basculegion-Female
        { (916, 1), 10254 }, // Oinkologne-Female

        // ── Gen 9 alternate forms ─────────────────────────────────────────────
        // Maushold: PKHeX form 0=Family-of-Three (PokeAPI 10257), form 1=Family-of-Four (base 925.png).
        // Handled as a special case below because form 0 ≠ PokeAPI default.
        { (931, 1), 10260 }, { (931, 2), 10261 }, { (931, 3), 10262 }, // Squawkabilly Blue/Yellow/White
        { (964, 1), 10256 }, // Palafin-Hero
        { (978, 1), 10258 }, { (978, 2), 10259 }, // Tatsugiri-Droopy / -Stretchy
        { (982, 1), 10255 }, // Dudunsparce-Three-Segment

        // ── Paradox / late Gen 9 ──────────────────────────────────────────────
        { (901, 1), 10272  }, // Ursaluna-Bloodmoon
        { (905, 1), 10249  }, // Enamorus-Therian
        { (999, 1), 10263  }, // Gimmighoul-Roaming
        { (1017, 1), 10273 }, // Ogerpon-Wellspring-Mask
        { (1017, 2), 10274 }, // Ogerpon-Hearthflame-Mask
        { (1017, 3), 10275 }, // Ogerpon-Cornerstone-Mask
        { (1024, 1), 10276 }, // Terapagos-Terastal
        { (1024, 2), 10277 }, // Terapagos-Stellar
    };

    // Species where ALL forms share a single home sprite (the base {species}.png).
    // Confirmed by checking the actual files at sprites/pokemon/other/home/.
    private static readonly HashSet<ushort> PokeApiFormIndifferentSpecies =
    [
        (ushort)Species.Scatterbug, // 664 — 20 Vivillon patterns, only 664.png exists
        (ushort)Species.Spewpa,     // 665 — 20 Vivillon patterns, only 665.png exists
    ];

    // 10xxx PokeAPI IDs that also have a female/ subdirectory sprite (female/{id}.png).
    // Currently only Sneasel-Hisui female (10235) is known to exist.
    private static readonly HashSet<uint> FemaleFormIds = [10235];

    // Species with gender-specific home sprites in PokeAPI's female/ subdirectory.
    // Derived from the actual files at sprites/pokemon/other/home/female/.
    // Includes gender-as-form species (Meowstic 678, Indeedee 876, Basculegion 902, Oinkologne 916)
    // whose female form is accessed via female/{species}.png even though PKHeX encodes gender as form.
    private static readonly HashSet<ushort> FemaleFormSpecies =
    [
        3, 12, 19, 20, 25, 26, 41, 42, 44, 45, 64, 65, 84, 85, 97, 111, 112,
        118, 119, 123, 129, 130, 133, 154, 165, 166, 178, 185, 186, 190, 194, 195,
        198, 202, 203, 207, 208, 212, 214, 215, 217, 221, 224, 229, 232,
        255, 256, 257, 267, 269, 272, 274, 275, 307, 308, 315, 316, 317,
        322, 323, 332, 350, 369, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 407,
        415, 417, 418, 419, 424, 443, 444, 445, 449, 450, 453, 454, 456, 457, 459, 460,
        461, 464, 465, 473, 521, 592, 593, 668, 678, 876, 902, 916,
    ];

    /// <summary>
    /// Returns <see langword="true"/> if PokeAPI hosts a gender-specific home sprite for this species
    /// in the <c>female/</c> subdirectory.
    /// </summary>
    public static bool HasFemaleHomeSprite(ushort species, byte gender) =>
        gender == (byte)Gender.Female && FemaleFormSpecies.Contains(species);

    /// <summary>
    /// Returns <see langword="true"/> if the given game version's sprite directory on PokeAPI
    /// includes a <c>shiny/</c> subdirectory.
    /// Gen I (shiny didn't exist), Gen VIII BDSP, and Gen IX do not have one in the CDN repo.
    /// </summary>
    private static bool HasShinyCdnSprite(GameVersion version) => version switch
    {
        // Gen I: shiny mechanic didn't exist
        GameVersion.RD or GameVersion.GN or GameVersion.BU
            or GameVersion.RB or GameVersion.RBY
            or GameVersion.YW => false,
        // Gen VIII BDSP: PokeAPI CDN has no shiny/ subdirectory
        GameVersion.BD or GameVersion.SP => false,
        // Gen IX: PokeAPI CDN has no shiny/ subdirectory
        GameVersion.SL or GameVersion.VL => false,
        _ => true
    };

    /// <summary>
    /// Returns <see langword="true"/> if the given game version's sprite directory on PokeAPI
    /// includes a <c>female/</c> subdirectory for gender-specific sprites.
    /// Gen I, II, III, and VIII (SwSh/PLA/BDSP) do not have one.
    /// </summary>
    private static bool HasFemaleGameSprite(GameVersion version) => version switch
    {
        // Gen IV — SAV4DP/HGSS report combined GameVersion.DP / GameVersion.HGSS
        GameVersion.D or GameVersion.P or GameVersion.DP
            or GameVersion.Pt
            or GameVersion.HG or GameVersion.SS or GameVersion.HGSS => true,
        GameVersion.B or GameVersion.W or GameVersion.B2 or GameVersion.W2 => true,
        GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS => true,
        GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM
            or GameVersion.GP or GameVersion.GE => true,
        GameVersion.SL or GameVersion.VL => true,
        _ => false
    };

    /// <summary>
    /// Maps a <see cref="GameVersion"/> to its PokeAPI versions/ subdirectory path segment,
    /// or <see langword="null"/> if no game-specific sprite directory exists (e.g. SwSh, PLA).
    /// </summary>
    private static string? GetPokeApiVersionPath(GameVersion version) => version switch
    {
        // Gen I — SAV1 reports GameVersion.RB for Red/Blue (after game detection) or RBY as fallback
        GameVersion.RD or GameVersion.GN or GameVersion.BU
            or GameVersion.RB or GameVersion.RBY => "generation-i/red-blue",
        GameVersion.YW => "generation-i/yellow",
        // Gen II — SAV2 reports GameVersion.GS for Gold/Silver (combined); use gold as the target dir
        GameVersion.GD or GameVersion.GS => "generation-ii/gold",
        GameVersion.SI => "generation-ii/silver",
        GameVersion.C => "generation-ii/crystal",
        // Gen III — SAV3RS reports GameVersion.RS for Ruby/Sapphire (combined)
        GameVersion.R or GameVersion.S or GameVersion.RS => "generation-iii/ruby-sapphire",
        GameVersion.E => "generation-iii/emerald",
        GameVersion.FR or GameVersion.LG => "generation-iii/firered-leafgreen",
        // Gen IV — SAV4DP reports GameVersion.DP, SAV4HGSS reports GameVersion.HGSS (both combined)
        GameVersion.D or GameVersion.P or GameVersion.DP => "generation-iv/diamond-pearl",
        GameVersion.Pt => "generation-iv/platinum",
        GameVersion.HG or GameVersion.SS or GameVersion.HGSS => "generation-iv/heartgold-soulsilver",
        // Gen V — PokeAPI has no separate B2W2 dir; BW is used for both
        GameVersion.B or GameVersion.W
            or GameVersion.B2 or GameVersion.W2 => "generation-v/black-white",
        // Gen VI
        GameVersion.X or GameVersion.Y => "generation-vi/x-y",
        GameVersion.OR or GameVersion.AS => "generation-vi/omegaruby-alphasapphire",
        // Gen VII — PokeAPI has only USUM; used for SM, USUM, and Let's Go
        GameVersion.SN or GameVersion.MN
            or GameVersion.US or GameVersion.UM
            or GameVersion.GP or GameVersion.GE => "generation-vii/ultra-sun-ultra-moon",
        // Gen VIII — BDSP only; SwSh and PLA return null so the component falls back to Home sprites
        GameVersion.BD or GameVersion.SP => "generation-viii/brilliant-diamond-shining-pearl",
        // Gen IX
        GameVersion.SL or GameVersion.VL => "generation-ix/scarlet-violet",
        _ => null
    };

    /// <summary>
    /// Gets the game-version-appropriate PokeAPI sprite URL for a Pokémon.
    /// Uses the pixel-art sprite from the save file's specific game directory on the PokeAPI CDN.
    /// Returns null when the version has no PokeAPI sprite directory (e.g. SwSh, PLA) or the
    /// species/form has no sprite in that generation — callers should fall back to the home sprite or
    /// the bundled sprite in those cases.
    /// </summary>
    public static string? GetPokeApiVersionSpriteUrl(ushort species, byte form = 0, uint? formArg = null,
        bool isShiny = false, byte gender = 0, GameVersion version = GameVersion.Any)
    {
        if (!species.IsValidSpecies())
            return null;

        var versionPath = GetPokeApiVersionPath(version);
        if (versionPath is null)
            return null;

        // Totem forms: map to their base regional/standard form for CDN sprite lookup.
        // e.g. Totem Raticate-Alola (form 2) → Raticate-Alola (form 1).
        if (FormInfo.HasTotemForm(species) && FormInfo.IsTotemForm(species, form))
            form = FormInfo.GetTotemBaseForm(species, form);

        var baseUrl = $"{PokeApiVersionsBaseUrl}{versionPath}/";
        // Some CDN directories lack a shiny/ subdirectory (Gen I, BDSP, Gen IX).
        // For those, serve the non-shiny CDN sprite rather than falling back to bundled.
        var shinyPath = isShiny && HasShinyCdnSprite(version) ? "shiny/" : "";
        var canUseFemale = HasFemaleGameSprite(version);

        // Alcremie: build named-form URL (same naming convention as HOME sprites)
        if (species == (ushort)Species.Alcremie)
        {
            var creamIdx = form < AlcremieCreamNames.Length ? form : 0;
            var sweetIdx = formArg is { } arg && arg < AlcremieSweetNames.Length ? (int)arg : 0;
            return $"{baseUrl}{shinyPath}{species}-{AlcremieCreamNames[creamIdx]}-{AlcremieSweetNames[sweetIdx]}.png";
        }

        // Forms stored as named suffix files (e.g. 201-a.png, 201-b.png)
        if (PokeApiFormSuffixes.TryGetValue((species, form), out var s))
        {
            var femaleSuffixPath = canUseFemale && HasFemaleHomeSprite(species, gender) ? "female/" : "";
            return $"{baseUrl}{shinyPath}{femaleSuffixPath}{species}-{s}.png";
        }

        // Forms stored as PokeAPI pokemon IDs (Megas, regionals, gender-as-form, and others)
        if (PokeApiFormIds.TryGetValue((species, form), out var pokeApiId))
        {
            var femaleIdPath = canUseFemale && gender == (byte)Gender.Female && FemaleFormIds.Contains(pokeApiId)
                ? "female/"
                : "";
            return $"{baseUrl}{shinyPath}{femaleIdPath}{pokeApiId}.png";
        }

        // Maushold: PKHeX form 0 = Family-of-Three (PokeAPI 10257), form 1 = Family-of-Four (base 925)
        if (species == (ushort)Species.Maushold)
        {
            var mausholdId = form == 0 ? "10257" : "925";
            return $"{baseUrl}{shinyPath}{mausholdId}.png";
        }

        // Base form (form 0) not in any form dictionary: use species number directly
        if (form == 0)
        {
            var femaleBasePath = canUseFemale && HasFemaleHomeSprite(species, gender) ? "female/" : "";
            return $"{baseUrl}{shinyPath}{femaleBasePath}{species}.png";
        }

        // Species where all forms share a single sprite: use base species URL
        if (PokeApiFormIndifferentSpecies.Contains(species))
        {
            var femaleIndPath = canUseFemale && HasFemaleHomeSprite(species, gender) ? "female/" : "";
            return $"{baseUrl}{shinyPath}{femaleIndPath}{species}.png";
        }

        // form > 0 not in any mapping: no game sprite — fall back to home sprite or bundled
        return null;
    }

    /// <summary>
    /// Gets the high-resolution PokeAPI home sprite URL for a Pokémon.
    /// Handles form variants, gender differences, shiny variants, and Alcremie decorations.
    /// Returns null for invalid species or forms with no PokeAPI home sprite.
    /// Use as a lazy-load upgrade over the bundled fallback sprite.
    /// </summary>
    public static string? GetPokeApiHomeSpriteUrl(ushort species, byte form = 0, uint? formArg = null,
        bool isShiny = false, byte gender = 0)
    {
        if (!species.IsValidSpecies())
            return null;

        var shinyPath = isShiny ? "shiny/" : "";

        // Totem forms: map to their base regional/standard form for CDN sprite lookup.
        if (FormInfo.HasTotemForm(species) && FormInfo.IsTotemForm(species, form))
            form = FormInfo.GetTotemBaseForm(species, form);

        // Alcremie: 9 cream forms (PKHeX form) × 7 sweets (PKHeX formArg)
        if (species == (ushort)Species.Alcremie)
        {
            var creamIdx = form < AlcremieCreamNames.Length ? form : 0;
            var sweetIdx = formArg is { } arg && arg < AlcremieSweetNames.Length ? (int)arg : 0;
            return $"{PokeApiHomeBaseUrl}{shinyPath}{species}-{AlcremieCreamNames[creamIdx]}-{AlcremieSweetNames[sweetIdx]}.png";
        }

        // Forms stored as named suffix files (e.g. 201-b.png, 666-icy-snow.png)
        if (PokeApiFormSuffixes.TryGetValue((species, form), out var s))
        {
            var femaleSuffixPath = HasFemaleHomeSprite(species, gender) ? "female/" : "";
            return $"{PokeApiHomeBaseUrl}{shinyPath}{femaleSuffixPath}{species}-{s}.png";
        }

        // Forms stored as PokeAPI pokemon IDs (Megas, regionals, gender-as-form, and others).
        // Also covers form 0 overrides where PKHeX form 0 ≠ PokeAPI's default (e.g. Maushold).
        if (PokeApiFormIds.TryGetValue((species, form), out var pokeApiId))
        {
            // A small number of IDs also have a female/ subdirectory sprite.
            var femaleIdPath = gender == (byte)Gender.Female && FemaleFormIds.Contains(pokeApiId)
                ? "female/"
                : "";
            return $"{PokeApiHomeBaseUrl}{shinyPath}{femaleIdPath}{pokeApiId}.png";
        }

        // Maushold: PKHeX form 0 = Family-of-Three (PokeAPI 10257), form 1 = Family-of-Four (base 925).
        // PKHeX indexes the less-common form first, opposite of PokeAPI's default.
        if (species == (ushort)Species.Maushold)
        {
            var mausholdId = form == 0 ? "10257" : "925";
            return $"{PokeApiHomeBaseUrl}{shinyPath}{mausholdId}.png";
        }

        // Base form (form 0) not in any form dictionary: use species number directly.
        // For gender-as-form species (Meowstic, Indeedee, Basculegion, Oinkologne), form 1 (female)
        // is in PokeApiFormIds above; their form 0 (male) falls through to here.
        if (form == 0)
        {
            var femaleBasePath = HasFemaleHomeSprite(species, gender) ? "female/" : "";
            return $"{PokeApiHomeBaseUrl}{shinyPath}{femaleBasePath}{species}.png";
        }

        // Species where all forms legitimately share a single home sprite: use base species URL.
        if (PokeApiFormIndifferentSpecies.Contains(species))
        {
            var femaleIndPath = HasFemaleHomeSprite(species, gender) ? "female/" : "";
            return $"{PokeApiHomeBaseUrl}{shinyPath}{femaleIndPath}{species}.png";
        }

        // form > 0 not in any mapping: no PokeAPI home sprite exists (Sinistea-Antique,
        // Rockruff-Own-Tempo, Ogerpon Tera forms, GMax, etc.).
        // Return null so the caller falls back to the bundled low-res sprite.
        return null;
    }

    /// <summary>Fallback image path for unknown items.</summary>
    public const string ItemFallbackImageFileName = $"{SpritesRoot}bi/bitem_unk.png";

    /// <summary>Fallback image path for unknown Pokémon.</summary>
    public const string PokemonFallbackImageFileName = $"{SpritesRoot}a/a_unknown.png";

    // Mail item IDs for different generations
    private static readonly int[] Gen2MailIds = [0x9E, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD];
    private static readonly int[] Gen3MailIds = [121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132];
    private static readonly int[] Gen45MailIds = [137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148];

    /// <summary>
    /// Gets the sprite filename for a Mystery Gift (either Pokémon or item).
    /// </summary>
    public static string GetMysteryGiftSpriteFileName(MysteryGift gift) => gift.IsItem
        ? GetItemSpriteFilename(gift.ItemID, gift.Context)
        : GetPokemonSpriteFilename(gift.Species, gift.Context, gift.IsEgg, gift.Form, 0, gift.Gender);

    /// <summary>
    /// Gets the sprite filename for a Pokémon, handling all forms, genders, and special cases.
    /// </summary>
    public static string GetPokemonSpriteFilename(PKM? pokemon) => pokemon is null
        ? PokemonFallbackImageFileName
        : GetPokemonSpriteFilename(pokemon.Species, pokemon.Context, pokemon.IsEgg, pokemon.Form,
            pokemon.GetFormArgument(0), pokemon.Gender);

    /// <summary>
    /// Internal method to construct the Pokémon sprite filename based on various attributes.
    /// Handles special cases like starter Pikachu/Eevee, eggs, gender differences, Alcremie variations, etc.
    /// </summary>
    private static string GetPokemonSpriteFilename(ushort species, EntityContext context, bool isEgg, byte form,
        uint? formArg1, byte gender) =>
        new StringBuilder($"{SpritesRoot}a/a_")
            .Append((species, context, isEgg, form, formArg1, gender) switch
            {
                // Let's Go starter forms with partner ribbon
                { context: EntityContext.Gen7b } and ({ species: (ushort)Species.Pikachu, form: PikachuStarterForm }
                    or { species: (ushort)Species.Eevee, form: EeveeStarterForm }) => $"{species}-{form}p",
                // Frillish and Jellicent have gender differences
                {
                        species: (ushort)Species.Frillish or (ushort)Species.Jellicent, gender: (byte)Gender.Female
                    } => $"{species}f",
                // Alcremie has form and decoration variations
                { species: (ushort)Species.Alcremie } => $"{species}-{form}-{formArg1}",
                // Handle Totem forms by mapping to base form
                { form: > 0 } when FormInfo.HasTotemForm(species) && FormInfo.IsTotemForm(species, form) =>
                    $"{species}-{FormInfo.GetTotemBaseForm(species, form)}",
                // Species with forms that should use base sprite
                { form: > 0 } => species switch
                {
                    (ushort)Species.Rockruff => species.ToString(),
                    (ushort)Species.Sinistea or (ushort)Species.Polteageist => species.ToString(),
                    (ushort)Species.Scatterbug or (ushort)Species.Spewpa => species.ToString(),
                    (ushort)Species.Urshifu => species.ToString(),
                    (ushort)Species.Dudunsparce => species.ToString(),
                    _ => $"{species}-{form}"
                },
                // Valid species with form 0
                { species: var speciesId } when speciesId.IsValidSpecies() =>
                    species.ToString(),
                // Fallback for invalid species
                _ => "unknown"
            })
            .Append(".png")
            .ToString();

    /// <summary>
    /// Gets the sprite filename for a Poké Ball.
    /// </summary>
    /// <param name="ball">The ball ID.</param>
    public static string GetBallSpriteFilename(int ball) =>
        $"{SpritesRoot}b/_ball{ball}.png";

    /// <summary>
    /// Gets the sprite filename for an item, selecting appropriate size/style based on generation.
    /// </summary>
    public static string GetItemSpriteFilename(int item, EntityContext context) => context switch
    {
        EntityContext.Gen1 or EntityContext.Gen2 => ItemFallbackImageFileName, // TODO: Fix Gen I and II item sprites
        EntityContext.Gen3 => ItemFallbackImageFileName, // TODO: Fix Gen III item sprites
        EntityContext.Gen9 or EntityContext.Gen9a => GetArtworkItemSpriteFilename(item, context),
        _ => GetBigItemSpriteFilename(item, context)
    };

    /// <summary>Gets the big item sprite filename (used for Gen 4-8).</summary>
    private static string GetBigItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}bi/bitem_{GetItemIdString(item, context)}.png";

    /// <summary>Gets the artwork item sprite filename (used for Gen 9).</summary>
    private static string GetArtworkItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}ai/aitem_{GetItemIdString(item, context)}.png";

    /// <summary>Gets the sprite filename for a type gem (used in type displays).</summary>
    public static string GetTypeGemSpriteFileName(byte type) =>
        $"{SpritesRoot}t/g/gem_{type:00}.png";

    /// <summary>Gets the sprite filename for a square type icon.</summary>
    public static string GetTypeSquareSpriteFileName(byte type) =>
        $"{SpritesRoot}t/s/type_icon_{type:00}.png";

    /// <summary>Gets the sprite filename for a square type icon.</summary>
    public static string GetTypeSquareSpriteFileName(int type) =>
        $"{SpritesRoot}t/s/type_icon_{type:00}.png";

    /// <summary>Gets the sprite filename for a wide type icon.</summary>
    public static string GetTypeWideSpriteFileName(byte type) =>
        $"{SpritesRoot}t/w/type_wide_{type:00}.png";

    /// <summary>Gets the sprite filename for a bag pouch icon.</summary>
    public static string GetBagPouchSpriteFileName(InventoryType type) =>
        $"{SpritesRoot}bag/Bag_{GetBagPouchSpriteName(type)}.png";

    /// <summary>Maps inventory types to bag pouch sprite names.</summary>
    private static string GetBagPouchSpriteName(InventoryType type) => type switch
    {
        InventoryType.Balls => "Balls",
        InventoryType.BattleItems => "Battle",
        InventoryType.Berries => "Berries",
        InventoryType.Candy => "Candy",
        InventoryType.FreeSpace => "Free",
        InventoryType.Ingredients => "Ingredient",
        InventoryType.Items => "Items",
        InventoryType.KeyItems => "Key",
        InventoryType.MailItems => "Mail",
        InventoryType.Medicine => "Medicine",
        InventoryType.PCItems => "PCItems",
        InventoryType.TMHMs => "Tech",
        InventoryType.Treasure => "Treasure",
        InventoryType.ZCrystals => "Z",
        InventoryType.MegaStones => "Mega",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// Gets the sprite filename for a move category icon (Physical/Special/Status).
    /// </summary>
    public static string GetMoveCategorySpriteFileName(MoveCategory moveCategory) =>
        moveCategory switch
        {
            MoveCategory.Status => $"{SpritesRoot}move/Status.png",
            MoveCategory.Physical => $"{SpritesRoot}move/Physical.png",
            MoveCategory.Special => $"{SpritesRoot}move/Special.png",
            _ => throw new ArgumentOutOfRangeException(nameof(moveCategory), moveCategory, null)
        };

    /// <summary>Gets the sprite filename for a gender icon (Male=0, Female=1, Genderless=2).</summary>
    public static string GetGenderSpriteFileName(Gender gender) =>
        $"{SpritesRoot}ac/gender_{(int)gender}.png";

    /// <summary>
    /// Gets the CSS class to apply to a Pokémon slot based on whether it contains a valid Pokémon.
    /// </summary>
    public static string GetSpriteCssClass(PKM? pkm) => (pkm?.Species).IsValidSpecies()
        ? " slot-fill"
        : string.Empty;


    /// <summary>Determines if an item is a mail item based on its ID and context.</summary>
    private static bool IsItemMail(int item, EntityContext context) => context switch
    {
        EntityContext.Gen2 when Gen2MailIds.Contains(item) => true,
        EntityContext.Gen3 when Gen3MailIds.Contains(item) => true,
        EntityContext.Gen4 or EntityContext.Gen5 when Gen45MailIds.Contains(item) => true,
        _ => false
    };

    /// <summary>
    /// Converts an item ID to its string representation for sprite filenames.
    /// Handles lumped items (TMs, TRs) and mail items specially.
    /// </summary>
    private static string GetItemIdString(int item, EntityContext context) =>
        HeldItemLumpUtil.GetIsLump(item, context) switch
        {
            HeldItemLumpImage.TechnicalMachine => "tm",
            HeldItemLumpImage.TechnicalRecord => "tr",
            _ => IsItemMail(item, context)
                ? "unk"
                : item.ToString()
        };
}
