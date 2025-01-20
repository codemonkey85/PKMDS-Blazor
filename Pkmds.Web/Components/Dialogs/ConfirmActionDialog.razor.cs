namespace Pkmds.Web.Components.Dialogs;

public partial class ConfirmActionDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public string Title { get; set; } = "Confirm Action";

    [Parameter] public string Message { get; set; } = "Are you sure you want to perform this action?";

    [Parameter] public string ConfirmText { get; set; } = "Confirm";

    [Parameter] public string ConfirmIcon { get; set; } = Icons.Material.Filled.Check;

    [Parameter] public Color ConfirmColor { get; set; } = Color.Primary;

    [Parameter] public string CancelText { get; set; } = "Cancel";

    [Parameter] public string CancelIcon { get; set; } = Icons.Material.Filled.Clear;

    [Parameter] public Color CancelColor { get; set; } = Color.Secondary;

    [Parameter] public EventCallback<bool> OnConfirm { get; set; }

    private void Confirm()
    {
        OnConfirm.InvokeAsync(true);
        MudDialog?.Close();
    }

    private void Cancel()
    {
        OnConfirm.InvokeAsync(false);
        MudDialog?.Close();
    }
}
