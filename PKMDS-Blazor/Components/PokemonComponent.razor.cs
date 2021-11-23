using Microsoft.AspNetCore.Components;
using PKMDSData;
using PokeApiNet;

namespace PKMDSBlazor.Components;

public partial class PokemonComponent : ComponentBase
{
    [Parameter]
    public PokemonData Pokemon { get; set; }

    [Inject]
    PokeApiClient PokeApiClient { get; set; }

    public string PokemonName { get; set; }

    public string PokemonSprite { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }

    public async Task Refresh()
    {
        await GetPokemonName();
        await GetPokemonSprite();
    }

    public async Task GetPokemonName() => PokemonName = (await PokeApiClient.GetResourceAsync<PokemonSpecies>(Pokemon.NationalId)).Name;

    public async Task GetPokemonSprite()
    {
        Pokemon pokeApiResult = await PokeApiClient.GetResourceAsync<Pokemon>(Pokemon.NationalId);
        PokemonSprites sprites = pokeApiResult.Sprites;

        PokemonForm formApiResult = null;
        if (Pokemon.FormeIndex > 0)
        {
            formApiResult = await PokeApiClient.GetResourceAsync<PokemonForm>(pokeApiResult.Forms[Pokemon.FormeIndex].Name);
        }

        if (Pokemon.IsShiny)
        {
            if (Pokemon.IsFemale)
            {
                PokemonSprite = formApiResult?.Sprites?.FrontShiny ?? sprites.FrontShinyFemale;
            }
            else
            {
                PokemonSprite = formApiResult?.Sprites?.FrontShiny ?? sprites.FrontShiny;
            }
        }
        else
        {
            if (Pokemon.IsFemale)
            {
                PokemonSprite = formApiResult?.Sprites?.FrontDefault ?? sprites.FrontFemale;
            }
            else
            {
                PokemonSprite = formApiResult?.Sprites?.FrontDefault ?? sprites.FrontDefault;
            }
        }
    }
}
