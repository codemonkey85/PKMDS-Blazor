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
            Console.WriteLine($"Species {i:000} seen: {saveFile.GetSeen(i)}");

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

    private void FillPokedex()
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
                FillGen8LaPokedex(la);
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

    // ReSharper disable UnusedParameter.Local
    private void FillGen4Pokedex(SAV4 s4) => ShowNotImplementedMessage();

    private void FillGen5Pokedex(SAV5 s5) => ShowNotImplementedMessage();

    private void FillGen6XyPokedex(SAV6XY xy) => ShowNotImplementedMessage();

    private void FillGen6AoPokedex(SAV6AO ao) => ShowNotImplementedMessage();

    private void FillGen7Pokedex(SAV7 s7) => ShowNotImplementedMessage();

    // ReSharper disable once InconsistentNaming
    private void FillGen7bPokedex(SAV7b b7) => ShowNotImplementedMessage();

    private void FillGen8SwShPokedex(SAV8SWSH swsh) => ShowNotImplementedMessage();

    private void FillGen8BsPokedex(SAV8BS bs) => ShowNotImplementedMessage();

    private void FillGen8LaPokedex(SAV8LA la) => ShowNotImplementedMessage();

    private void FillGen9Rev0Pokedex(SAV9SV sv) => ShowNotImplementedMessage();

    private void FillGen9Rev1Pokedex(SAV9SV sv) => ShowNotImplementedMessage();
    // ReSharper restore UnusedParameter.Local

    private void ShowNotImplementedMessage() =>
        DialogService.ShowMessageBox(
            "Not implemented",
            "This feature is not implemented yet. Please check back later.",
            options: new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small });
}
