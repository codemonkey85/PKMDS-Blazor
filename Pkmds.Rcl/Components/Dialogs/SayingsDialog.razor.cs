namespace Pkmds.Rcl.Components.Dialogs;

public partial class SayingsDialog
{
    // PKHeX SAV6.LongStringLength = 0x22 bytes = 17 wide chars max per Saying.
    private const int SayingMaxChars = 17;

    // MyStatus6XY.Nickname max is 12 chars (StringConverter6 limit).
    private const int NicknameMaxChars = 12;

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

    private void Confirm()
    {
        if (Status is null)
        {
            MudDialog?.Close(DialogResult.Cancel());
            return;
        }

        Status.Saying1 = sayings[0];
        Status.Saying2 = sayings[1];
        Status.Saying3 = sayings[2];
        Status.Saying4 = sayings[3];
        Status.Saying5 = sayings[4];

        if (XyNickname is not null)
        {
            XyNickname.Nickname = nickname;
        }

        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
