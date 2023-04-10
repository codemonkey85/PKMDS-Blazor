namespace Pkmds.Rcl.Components.EditForms.Pokemon;

public partial class Gen5PokemonEditForm : IDisposable
{
    protected override void OnInitialized() => AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() => AppState.OnAppStateChanged -= StateHasChanged;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<IEnumerable<ushort>> SearchPokemonNames(string value)
    {
        var take = AppState.SaveFile?.Context switch
        {
            EntityContext.Gen1 => Species.Mew,
            EntityContext.Gen2 => Species.Celebi,
            EntityContext.Gen3 => Species.Deoxys,
            EntityContext.Gen4 => Species.Arceus,
            EntityContext.Gen5 => Species.Genesect,
            EntityContext.Gen6 => Species.Volcanion,
            EntityContext.Gen7 => Species.Marshadow,
            EntityContext.Gen7b => Species.Melmetal,
            EntityContext.Gen8 => Species.Eternatus,
            EntityContext.Gen8a => Species.Calyrex,
            EntityContext.Gen8b => Species.Enamorus,
            EntityContext.Gen9 => Species.IronLeaves,
            null => throw new NotImplementedException(),
            _ => Species.MAX_COUNT - 1,
        };
        return AppState.SpeciesNameDictionary
            .Take((ushort)take)
            .Where(kvp => kvp.Value.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => (ushort)kvp.Key);
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    private string ConvertSpeciesToName(ushort species) =>
        AppState.SpeciesNameDictionary[(Species)species];
}
