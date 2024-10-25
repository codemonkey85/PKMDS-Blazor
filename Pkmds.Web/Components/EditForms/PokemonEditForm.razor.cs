namespace Pkmds.Web.Components.EditForms;

public partial class PokemonEditForm : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private void ExportAsShowdown() =>
        DialogService.Show<ShowdownExportDialog>(
            "Showdown Export",
            new DialogParameters
            {
                { nameof(ShowdownExportDialog.Pokemon), Pokemon }
            },
            new DialogOptions
            {
                CloseOnEscapeKey = true,
            });

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
            { nameof(ConfirmActionDialog.CancelColor), Color.Error},
            { nameof(ConfirmActionDialog.OnConfirm), EventCallback.Factory.Create<bool>(this, OnDeleteConfirm) }
        };

        DialogService.Show<ConfirmActionDialog>(
            "Confirm Action",
            parameters,
            new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
            });

        void OnDeleteConfirm(bool confirmed)
        {

            if (!confirmed)
            {
                return;
            }

            if (AppState.SelectedPartySlotNumber is not null)
            {
                AppService.DeletePokemon(AppState.SelectedPartySlotNumber.Value);
            }
            else if (AppState.SelectedBoxNumber is not null && AppState.SelectedBoxSlotNumber is not null)
            {
                AppService.DeletePokemon(AppState.SelectedBoxNumber.Value, AppState.SelectedBoxSlotNumber.Value);
            }
        }
    }
}
