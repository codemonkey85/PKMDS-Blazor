namespace Pkmds.Rcl.Components.Dialogs;

public partial class AlphaMasteredDialog
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<AlphaMasteredInfo> AlphaMasteredMoves { get; set; } = [];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadAlphaMasteredMoves();
    }

    private void LoadAlphaMasteredMoves()
    {
        AlphaMasteredMoves.Clear();

        if (Pokemon is not IMoveShop8Mastery mastery)
        {
            return;
        }

        var moveNames = GameInfo.Strings.movelist;
        var context = Pokemon.Context;

        // Get the list of valid moves from the Permit interface (via IMoveShop8)
        if (Pokemon is not IMoveShop8 moveShop)
        {
            return;
        }

        var permit = moveShop.Permit;
        var indexes = permit.RecordPermitIndexes;

        for (var i = 0; i < indexes.Length; i++)
        {
            var moveId = indexes[i];
            var moveName = moveNames[moveId];
            var moveType = MoveInfo.GetType(moveId, context);
            var isMastered = mastery.GetMasteredRecordFlag(i);

            AlphaMasteredMoves.Add(new AlphaMasteredInfo
            {
                Index = i,
                MoveId = moveId,
                Name = moveName,
                Type = moveType,
                IsMastered = isMastered
            });
        }
    }

    private void ToggleAlphaMasteredFlag(int index, bool value)
    {
        var masteredMove = AlphaMasteredMoves.FirstOrDefault(m => m.Index == index);
        if (masteredMove != null)
        {
            masteredMove.IsMastered = value;
        }
    }

    private void GiveAll()
    {
        foreach (var masteredMove in AlphaMasteredMoves)
        {
            masteredMove.IsMastered = true;
        }
        StateHasChanged();
    }

    private void RemoveAll()
    {
        foreach (var masteredMove in AlphaMasteredMoves)
        {
            masteredMove.IsMastered = false;
        }
        StateHasChanged();
    }

    private void Save()
    {
        if (Pokemon is not IMoveShop8Mastery mastery)
        {
            return;
        }

        // Apply all alpha mastered flag changes to the Pokemon
        foreach (var masteredMove in AlphaMasteredMoves)
        {
            mastery.SetMasteredRecordFlag(masteredMove.Index, masteredMove.IsMastered);
        }

        RefreshService.Refresh();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel()
    {
        // Close without applying changes - alpha mastered flags were not modified
        MudDialog?.Close(DialogResult.Cancel());
    }

    public class AlphaMasteredInfo
    {
        public int Index { get; set; }
        public ushort MoveId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Type { get; set; }
        public bool IsMastered { get; set; }
    }
}
