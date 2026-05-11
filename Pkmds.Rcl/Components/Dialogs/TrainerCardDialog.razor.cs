namespace Pkmds.Rcl.Components.Dialogs;

public partial class TrainerCardDialog
{
    private string cardOt = string.Empty;
    private string cardNumber = string.Empty;
    private int trainerId;
    private int rotoRallyScore;

    [Parameter]
    [EditorRequired]
    public SAV8SWSH? Save { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    protected override void OnParametersSet()
    {
        if (Save is null)
        {
            return;
        }

        var card = Save.Blocks.TrainerCard;
        cardOt = card.OT;
        cardNumber = card.Number;
        trainerId = card.TrainerID;
        rotoRallyScore = card.RotoRallyScore;
    }

    private void Confirm()
    {
        if (Save is null)
        {
            MudDialog?.Close(DialogResult.Cancel());
            return;
        }

        var card = Save.Blocks.TrainerCard;
        if (card.OT != cardOt)
        {
            card.OT = cardOt;
        }

        // PKHeX writes Number to both MyStatus and TrainerCard to keep them in sync —
        // mismatch leaves the in-game card displaying a different number than the trainer.
        Save.Blocks.MyStatus.Number = cardNumber;
        card.Number = cardNumber;

        card.TrainerID = trainerId;
        // Setter auto-mirrors to the KRotoRally record block.
        card.RotoRallyScore = rotoRallyScore;

        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
