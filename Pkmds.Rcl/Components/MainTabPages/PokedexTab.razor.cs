namespace Pkmds.Rcl.Components.MainTabPages;

public partial class PokedexTab
{
    private int GetSeenCount()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return 0;
        }

        var count = 0;

        for (ushort i = 1; i < saveFile.MaxSpeciesID + 1; i++)
        {
            if (saveFile.GetSeen(i))
            {
                count++;
            }
        }

        return count;
    }

    private int GetCaughtCount()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return 0;
        }

        var count = 0;

        for (ushort i = 1; i < saveFile.MaxSpeciesID + 1; i++)
        {
            if (saveFile.GetCaught(i))
            {
                count++;
            }
        }

        return count;
    }

    // Clamp the raw seen/caught count to the dex total so the text and bar
    // never exceed 100 % even if the save contains species flagged as seen/caught
    // outside the game's formal Pokédex (e.g. anomalous HOME-transfer flags).
    private int GetDisplaySeenCount(SaveFile saveFile) =>
        Math.Min(GetSeenCount(), GetDexTotalCount(saveFile));

    private int GetDisplayCaughtCount(SaveFile saveFile) =>
        Math.Min(GetCaughtCount(), GetDexTotalCount(saveFile));

    private double GetSeenPercent()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return 0;
        }

        var total = GetDexTotalCount(saveFile);
        return total == 0 ? 0 : Math.Min(100.0, (double)GetSeenCount() / total * 100);
    }

    private double GetCaughtPercent()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return 0;
        }

        var total = GetDexTotalCount(saveFile);
        return total == 0 ? 0 : Math.Min(100.0, (double)GetCaughtCount() / total * 100);
    }

    // Returns the number of species that can actually appear in this game's Pokédex.
    // Each case mirrors the species enumeration logic used by the corresponding
    // PKHeX WinForms Pokédex editor (SAV_Pokedex*.cs) to populate its species list.
    private static int GetDexTotalCount(SaveFile saveFile)
    {
        switch (saveFile)
        {
            // SAV_PokedexGG: hardcoded list of 1–151 + Meltan (808) + Melmetal (809).
            // No HOME support — cross-gen species cannot exist in LGPE.
            case SAV7b:
                return 153;

            // SAV_PokedexSWSH: filters by Zukan8.GetEntry(), which checks the
            // species' regional dex index (Galar / Armor DLC / Crown DLC).
            // "Dexit" means many national species have no dex index and return false.
            case SAV8SWSH swsh:
            {
                var count = 0;
                for (ushort i = 1; i <= swsh.MaxSpeciesID; i++)
                {
                    if (swsh.Zukan.GetEntry(i, out _))
                    {
                        count++;
                    }
                }
                return count;
            }

            // SAV_PokedexLA: filters by PokedexSave8a.GetDexIndex(Hisui, species) != 0.
            // No HOME support — only Hisui-native species are tracked.
            case SAV8LA la:
            {
                var count = 0;
                for (ushort i = 1; i <= la.MaxSpeciesID; i++)
                {
                    if (PokedexSave8a.GetDexIndex(PokedexType8a.Hisui, i) != 0)
                    {
                        count++;
                    }
                }
                return count;
            }

            // SAV_PokedexSV: filters by Zukan9.GetDexIndex(species).Index != 0,
            // which covers all three regional dexes (Paldea / Kitakami / Blueberry).
            case SAV9SV sv:
            {
                var count = 0;
                for (ushort i = 1; i <= sv.MaxSpeciesID; i++)
                {
                    if (sv.Zukan.GetDexIndex(i).Index != 0)
                    {
                        count++;
                    }
                }
                return count;
            }

            // All other games (Gen 1–7, BDSP): every national species up to
            // MaxSpeciesID is present in the game, so MaxSpeciesID is exact.
            // BDSP (SAV8BS): MaxSpeciesID = 493 (Arceus); SAV_PokedexBDSP iterates
            // 1..MaxSpeciesID with no filtering, confirming all 493 are valid.
            default:
                return saveFile.MaxSpeciesID;
        }
    }

    // Gen 8 LA uses PokedexSave8a (no Zukan); SeenAll and Clear are not applicable.
    private bool IsLegendArceus => AppState.SaveFile is SAV8LA;

    private async Task FillPokedex()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return;
        }

        switch (saveFile)
        {
            case SAV1 s1:
                FillGen1Pokedex(s1);
                break;
            case SAV2 s2:
                FillGen2Pokedex(s2);
                break;
            case SAV3 s3:
                FillGen3Pokedex(s3);
                break;
            case SAV4 s4:
                FillGen4Pokedex(s4);
                break;
            case SAV5 s5:
                FillGen5Pokedex(s5);
                break;
            case SAV6XY xy:
                FillGen6XyPokedex(xy);
                break;
            case SAV6AO ao:
                FillGen6AoPokedex(ao);
                break;
            case SAV7 s7:
                FillGen7Pokedex(s7);
                break;
            case SAV7b b7:
                FillGen7bPokedex(b7);
                break;
            case SAV8SWSH swsh:
                FillGen8SwShPokedex(swsh);
                break;
            case SAV8BS bs:
                FillGen8BsPokedex(bs);
                break;
            case SAV8LA la:
                await FillGen8LaPokedex(la);
                break;
            case SAV9SV { SaveRevision: 0 } sv:
                FillGen9Rev0Pokedex(sv);
                break;
            case SAV9SV sv:
                FillGen9Rev1Pokedex(sv);
                break;
        }
    }

    private static void FillGen1Pokedex(SAV1 s1)
    {
        for (ushort i = 1; i < s1.MaxSpeciesID + 1; i++)
        {
            s1.SetSeen(i, true); // Set all Pokémon as seen
            s1.SetCaught(i, true); // Set all Pokémon as caught
        }
    }

    private static void FillGen2Pokedex(SAV2 s2)
    {
        for (ushort i = 1; i < s2.MaxSpeciesID + 1; i++)
        {
            s2.SetSeen(i, true); // Set all Pokémon as seen
            s2.SetCaught(i, true); // Set all Pokémon as caught
        }
    }

    private static void FillGen3Pokedex(SAV3 s3)
    {
        for (ushort i = 1; i < s3.MaxSpeciesID + 1; i++)
        {
            s3.SetSeen(i, true); // Set all Pokémon as seen
            s3.SetCaught(i, true); // Set all Pokémon as caught
        }
    }

    // NuGet 26.1.31: Zukan4.CompleteDex requires explicit shinyToo argument.
    private static void FillGen4Pokedex(SAV4 s4) => s4.Dex.CompleteDex(false);

    // NuGet 26.1.31: Zukan5 has no CompleteDex(); iterate via GiveAll per species.
    private static void FillGen5Pokedex(SAV5 s5)
    {
        for (ushort i = 1; i < s5.MaxSpeciesID + 1; i++)
        {
            s5.Zukan.GiveAll(i, true, false, LanguageID.English, false);
        }
    }

    // NuGet 26.1.31: Zukan6XY has no CompleteDex(); iterate via GiveAll per species.
    private static void FillGen6XyPokedex(SAV6XY xy)
    {
        for (ushort i = 1; i < xy.MaxSpeciesID + 1; i++)
        {
            xy.Zukan.GiveAll(i, true, false, LanguageID.English, false);
        }
    }

    // NuGet 26.1.31: Zukan6AO has no CompleteDex(); iterate via GiveAll per species.
    private static void FillGen6AoPokedex(SAV6AO ao)
    {
        for (ushort i = 1; i < ao.MaxSpeciesID + 1; i++)
        {
            ao.Zukan.GiveAll(i, true, false, LanguageID.English, false);
        }
    }

    // NuGet 26.1.31: Zukan7.CompleteDex requires explicit shinyToo argument.
    private static void FillGen7Pokedex(SAV7 s7) => s7.Zukan.CompleteDex(false);

    // NuGet 26.1.31: Zukan7b.CompleteDex requires explicit shinyToo argument.
    // ReSharper disable once InconsistentNaming
    private static void FillGen7bPokedex(SAV7b b7) => b7.Zukan.CompleteDex(false);

    private static void FillGen8SwShPokedex(SAV8SWSH swsh) => swsh.Zukan.CompleteDex();

    private static void FillGen8BsPokedex(SAV8BS bs) => bs.Zukan.CompleteDex();

    // Gen 8 LA has no CompleteDex(); force-complete each research task per species instead.
    private static async Task FillGen8LaPokedex(SAV8LA la)
    {
        la.PokedexSave.SetSolitudeAll();

        for (ushort species = 1; species <= la.MaxSpeciesID; species++)
        {
            var dexIndex = PokedexSave8a.GetDexIndex(PokedexType8a.Hisui, species);
            if (dexIndex == 0)
            {
                continue;
            }

            foreach (var task in PokedexConstants8a.ResearchTasks[dexIndex - 1])
            {
                if (task.TaskThresholds.Length == 0)
                {
                    continue;
                }

                la.PokedexSave.SetResearchTaskProgressByForce(species, task, task.TaskThresholds[^1]);
            }

            if (species % 50 == 0)
            {
                await Task.Yield();
            }
        }

        la.PokedexSave.UpdateAllReportPoke();
    }

    private static void FillGen9Rev0Pokedex(SAV9SV sv) => sv.Zukan.CompleteDex();

    private static void FillGen9Rev1Pokedex(SAV9SV sv) => sv.Zukan.CompleteDex();

    private void SeenAllPokedex()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return;
        }

        switch (saveFile)
        {
            case SAV1 s1:
                for (ushort i = 1; i < s1.MaxSpeciesID + 1; i++)
                {
                    s1.SetSeen(i, true);
                }
                break;
            case SAV2 s2:
                for (ushort i = 1; i < s2.MaxSpeciesID + 1; i++)
                {
                    s2.SetSeen(i, true);
                }
                break;
            case SAV3 s3:
                for (ushort i = 1; i < s3.MaxSpeciesID + 1; i++)
                {
                    s3.SetSeen(i, true);
                }
                break;
            // NuGet 26.1.31: Gen 4–7b SeenAll requires explicit shinyToo argument.
            case SAV4 s4:
                s4.Dex.SeenAll(false);
                break;
            case SAV5 s5:
                s5.Zukan.SeenAll(false);
                break;
            case SAV6XY xy:
                xy.Zukan.SeenAll(false);
                break;
            case SAV6AO ao:
                ao.Zukan.SeenAll(false);
                break;
            case SAV7 s7:
                s7.Zukan.SeenAll(false);
                break;
            case SAV7b b7:
                b7.Zukan.SeenAll(false);
                break;
            case SAV8SWSH swsh:
                swsh.Zukan.SeenAll();
                break;
            case SAV8BS bs:
                bs.Zukan.SeenAll();
                break;
            // SAV8LA: no Zukan.SeenAll(); button is disabled for LA
            case SAV9SV sv:
                sv.Zukan.SeenAll();
                break;
        }
    }

    private void ClearPokedex()
    {
        if (AppState.SaveFile is not { HasPokeDex: true } saveFile)
        {
            return;
        }

        switch (saveFile)
        {
            case SAV1 s1:
                for (ushort i = 1; i < s1.MaxSpeciesID + 1; i++)
                {
                    s1.SetSeen(i, false);
                    s1.SetCaught(i, false);
                }
                break;
            case SAV2 s2:
                for (ushort i = 1; i < s2.MaxSpeciesID + 1; i++)
                {
                    s2.SetSeen(i, false);
                    s2.SetCaught(i, false);
                }
                break;
            case SAV3 s3:
                for (ushort i = 1; i < s3.MaxSpeciesID + 1; i++)
                {
                    s3.SetSeen(i, false);
                    s3.SetCaught(i, false);
                }
                break;
            case SAV4 s4:
                s4.Dex.SeenNone();
                s4.Dex.CaughtNone();
                break;
            case SAV5 s5:
                s5.Zukan.SeenNone();
                s5.Zukan.CaughtNone();
                break;
            case SAV6XY xy:
                xy.Zukan.SeenNone();
                xy.Zukan.CaughtNone();
                break;
            case SAV6AO ao:
                ao.Zukan.SeenNone();
                ao.Zukan.CaughtNone();
                break;
            case SAV7 s7:
                s7.Zukan.SeenNone();
                s7.Zukan.CaughtNone();
                break;
            case SAV7b b7:
                b7.Zukan.SeenNone();
                b7.Zukan.CaughtNone();
                break;
            case SAV8SWSH swsh:
                swsh.Zukan.SeenNone();
                swsh.Zukan.CaughtNone();
                break;
            case SAV8BS bs:
                bs.Zukan.SeenNone();
                bs.Zukan.CaughtNone();
                break;
            // SAV8LA: no Zukan; button is disabled for LA
            case SAV9SV sv:
                sv.Zukan.SeenNone();
                sv.Zukan.CaughtNone();
                break;
        }
    }
}
