namespace Pkmds.Rcl.Components.Dialogs;

public partial class SayingsDialog
{
    private readonly string[] sayings = new string[5];
    private string nickname = string.Empty;

    [Parameter]
    [EditorRequired]
    public MyStatus6? Status { get; set; }

    /// <summary>XY-only Trainer Card nickname. Pass null for ORAS (which has no separate nickname).</summary>
    [Parameter]
    public MyStatus6XY? XyNickname { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    protected override void OnParametersSet()
    {
        if (Status is null)
        {
            return;
        }

        sayings[0] = Status.Saying1;
        sayings[1] = Status.Saying2;
        sayings[2] = Status.Saying3;
        sayings[3] = Status.Saying4;
        sayings[4] = Status.Saying5;

        nickname = XyNickname?.Nickname ?? string.Empty;
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
