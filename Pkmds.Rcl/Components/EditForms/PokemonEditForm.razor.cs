namespace Pkmds.Rcl.Components.EditForms;

public partial class PokemonEditForm : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    private LegalityAnalysis? Analysis { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= Refresh;

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += Refresh;
        ComputeAnalysis();
    }

    protected override void OnParametersSet() => ComputeAnalysis();

    private void Refresh()
    {
        ComputeAnalysis();
        StateHasChanged();
    }

    private void ComputeAnalysis() =>
        Analysis = Pokemon is { Species: > 0 }
            ? AppService.GetLegalityAnalysis(Pokemon)
            : null;

    private void ExportAsShowdown() =>
        DialogService.ShowAsync<ShowdownExportDialog>(
            "Showdown Export",
            new() { { nameof(ShowdownExportDialog.Pokemon), Pokemon } },
            new() { CloseOnEscapeKey = true });

    private void SaveAndClose()
    {
        AppService.SavePokemon(Pokemon);
        AppService.ClearSelection();
    }

    private void DeletePokemon()
    {
        var parameters = new DialogParameters
        {
            { nameof(ConfirmActionDialog.Title), "Delete Pokémon" },
            { nameof(ConfirmActionDialog.Message), "Are you sure you want to delete this Pokémon?" },
            { nameof(ConfirmActionDialog.ConfirmText), "Delete" },
            { nameof(ConfirmActionDialog.ConfirmIcon), Icons.Material.Filled.Delete },
            { nameof(ConfirmActionDialog.ConfirmColor), Color.Default },
            { nameof(ConfirmActionDialog.CancelText), "Cancel" },
            { nameof(ConfirmActionDialog.CancelIcon), Icons.Material.Filled.Clear },
            { nameof(ConfirmActionDialog.CancelColor), Color.Error },
            { nameof(ConfirmActionDialog.OnConfirm), EventCallback.Factory.Create<bool>(this, OnDeleteConfirm) }
        };

        DialogService.ShowAsync<ConfirmActionDialog>(
            "Confirm Action",
            parameters,
            new() { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small });

        void OnDeleteConfirm(bool confirmed)
        {
            if (!confirmed)
            {
                return;
            }

            var selectedPokemonType =
                AppService.GetSelectedPokemonSlot(out var partySlot, out var boxNumber, out var boxSlot);
            switch (selectedPokemonType)
            {
                case SelectedPokemonType.Party:
                    AppService.DeletePokemon(partySlot);
                    break;
                case SelectedPokemonType.Box:
                    AppService.DeletePokemon(boxNumber, boxSlot);
                    break;
            }
        }
    }

    private void OnClickCopy()
    {
        if (Pokemon is null)
        {
            return;
        }

        AppState.CopiedPokemon = Pokemon.Clone();

        Snackbar.Add("The selected Pokémon has been copied.");
    }

    private void OnClickPaste()
    {
        if (AppState.CopiedPokemon is null)
        {
            return;
        }

        if ((Pokemon?.Species).IsValidSpecies())
        {
            ShowPasteConfirmation();
        }
        else
        {
            PastePokemon();
        }

        void ShowPasteConfirmation()
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmActionDialog.Title), "Paste Pokémon" },
                { nameof(ConfirmActionDialog.Message), "Are you sure you want to paste the copied Pokémon? The Pokémon in the selected slot will be replaced." },
                { nameof(ConfirmActionDialog.ConfirmText), "Paste" },
                { nameof(ConfirmActionDialog.ConfirmIcon), Icons.Material.Filled.ContentPaste },
                { nameof(ConfirmActionDialog.ConfirmColor), Color.Default },
                { nameof(ConfirmActionDialog.CancelText), "Cancel" },
                { nameof(ConfirmActionDialog.CancelIcon), Icons.Material.Filled.Clear },
                { nameof(ConfirmActionDialog.CancelColor), Color.Primary },
                { nameof(ConfirmActionDialog.OnConfirm), EventCallback.Factory.Create<bool>(this, OnPasteConfirm) }
            };

            DialogService.ShowAsync<ConfirmActionDialog>(
                "Confirm Action",
                parameters,
                new() { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small });
        }

        void OnPasteConfirm(bool confirmed)
        {
            if (!confirmed)
            {
                return;
            }

            PastePokemon();
        }

        void PastePokemon()
        {
            Pokemon = AppState.CopiedPokemon.Clone();
            AppService.SavePokemon(Pokemon);

            var selectedPokemonType =
                AppService.GetSelectedPokemonSlot(out var partySlot, out var boxNumber, out var boxSlot);
            switch (selectedPokemonType)
            {
                case SelectedPokemonType.Party:
                    AppService.SetSelectedPartyPokemon(Pokemon, partySlot);
                    break;
                case SelectedPokemonType.Box:
                    AppService.SetSelectedBoxPokemon(Pokemon, boxNumber, boxSlot);
                    break;
            }

            Snackbar.Add("The copied Pokémon has been pasted.");
        }
    }
}
