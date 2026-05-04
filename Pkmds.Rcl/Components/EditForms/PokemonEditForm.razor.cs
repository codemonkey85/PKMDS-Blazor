namespace Pkmds.Rcl.Components.EditForms;

public partial class PokemonEditForm : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Inject]
    private IBankService BankService { get; set; } = null!;

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

    private void OnPokemonLegalized(PKM legalPkm)
    {
        // Replace the edit form's Pokémon with the legalized clone and recompute analysis.
        Pokemon = legalPkm;
        AppService.EditFormPokemon = legalPkm;
        ComputeAnalysis();
        RefreshService.Refresh();
    }

    private async Task ExportAsShowdown()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<ShowdownExportDialog>(
            "Showdown Export",
            new() { { nameof(ShowdownExportDialog.Pokemon), Pokemon } },
            options);
    }

    private async Task ExportToPokePaste()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<PokePasteExportDialog>(
            "Export to PokePaste",
            new() { { nameof(PokePasteExportDialog.Pokemon), Pokemon } },
            options);
    }

    private async Task ImportFromShowdown()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<ShowdownImportDialog>(
            "Import from Showdown / PokePaste",
            new DialogParameters<ShowdownImportDialog>(),
            options);
    }

    private async Task AddToBankAsync()
    {
        if (Pokemon is null || Pokemon.Species.IsInvalidSpecies() || AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        if (AppService.EditFormHasUnsavedChanges())
        {
            var save = await DialogService.ShowMessageBoxAsync(
                "Unsaved Changes",
                "This Pokémon has unsaved changes. Save before adding to the Bank?",
                yesText: "Save & Bank",
                cancelText: "Cancel");

            if (save is not true)
            {
                return;
            }

            AppService.SavePokemon(Pokemon);
        }

        var isDuplicate = await BankService.IsDuplicateAsync(Pokemon);
        if (isDuplicate)
        {
            var addAnyway = await DialogService.ShowMessageBoxAsync(
                "Already in Bank",
                "This Pokémon is already in the Bank. Add it again?",
                yesText: "Add Again",
                cancelText: "Cancel");

            if (addAnyway is not true)
            {
                return;
            }
        }

        var tid = saveFile.DisplayTID.ToString(AppService.GetIdFormatString());
        var gameName = SaveFileNameDisplay.FriendlyGameName(saveFile.Version);
        var sourceSave = $"{saveFile.OT} ({tid}, {gameName})";

        await BankService.AddAsync(Pokemon, sourceSave: sourceSave);

        var speciesName = AppService.GetPokemonSpeciesName(Pokemon.Species)
                          ?? Pokemon.Species.ToString(CultureInfo.InvariantCulture);
        Snackbar.Add($"{speciesName} added to Bank.", Severity.Success);
    }

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
            { nameof(ConfirmActionDialog.ConfirmColor), Color.Error },
            { nameof(ConfirmActionDialog.CancelText), "Cancel" },
            { nameof(ConfirmActionDialog.CancelIcon), Icons.Material.Filled.Clear },
            { nameof(ConfirmActionDialog.CancelColor), Color.Default },
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
                { nameof(ConfirmActionDialog.ConfirmColor), Color.Primary },
                { nameof(ConfirmActionDialog.CancelText), "Cancel" },
                { nameof(ConfirmActionDialog.CancelIcon), Icons.Material.Filled.Clear },
                { nameof(ConfirmActionDialog.CancelColor), Color.Default },
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
            if (AppState.SaveFile is not { } saveFile || AppState.CopiedPokemon is null)
            {
                return;
            }

            var pasted = AppState.CopiedPokemon.Clone();

            // The copied Pokémon may originate from a different save loaded earlier in the
            // session (e.g. copy from a Gen 6 save, then load a Gen 5 Black 2 save). PKHeX's
            // SetPartySlot/SetBoxSlot throw ArgumentException when the runtime PKM type
            // doesn't match the save's PKMType, so convert first and surface a friendly error
            // when conversion isn't possible instead of crashing.
            if (pasted.GetType() != saveFile.PKMType)
            {
                var converted = EntityConverter.ConvertToType(pasted, saveFile.PKMType, out var c);
                if (!c.IsSuccess || converted is null)
                {
                    Snackbar.Add(
                        $"Could not paste Pokémon: {c.GetDisplayString(pasted, saveFile.PKMType)}",
                        Severity.Error);
                    return;
                }

                pasted = converted;
            }

            saveFile.AdaptToSaveFile(pasted);

            Pokemon = pasted;
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
