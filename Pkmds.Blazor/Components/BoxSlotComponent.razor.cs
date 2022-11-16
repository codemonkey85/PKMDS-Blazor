namespace Pkmds.Blazor.Components;

public partial class BoxSlotComponent
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    private string? spriteImageUrl;

    protected override async Task OnParametersSetAsync()
    {
        if (Pokemon is not { Species: > 0 })
        {
            return;
        }

        spriteImageUrl = (await PokeApiClient.GetResourceAsync<Pokemon>(Pokemon.Species)).Sprites.Other.Home.FrontDefault;
    }
}
