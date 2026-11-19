namespace Pkmds.Rcl.Components.Dialogs;

public partial class RelearnFlagsDialog
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<TrMoveInfo> TrMoves { get; } = [];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadTrMoves();
    }

    private void LoadTrMoves()
    {
        if (Pokemon is not ITechRecord techRecord)
        {
            return;
        }

        var permit = techRecord.Permit;
        var moveNames = GameInfo.Strings.movelist;
        var context = Pokemon.Context;

        TrMoves.Clear();

        var indexes = permit.RecordPermitIndexes;
        var baseRecordIndex = context == EntityContext.Gen9a
            ? 1
            : 0; // TM001 in Legends: Z-A but is 0-index bits

        for (var i = 0; i < indexes.Length; i++)
        {
            var recordIndex = i + baseRecordIndex;
            var moveId = indexes[i];
            var moveName = moveNames[moveId];
            var moveType = MoveInfo.GetType(moveId, context);
            var isLearned = techRecord.GetMoveRecordFlag(recordIndex);

            TrMoves.Add(new TrMoveInfo
            {
                Index = recordIndex,
                MoveId = moveId,
                Name = moveName,
                Type = moveType,
                IsLearned = isLearned
            });
        }
    }

    private void ToggleTrFlag(int index, bool value)
    {
        var trMove = TrMoves.FirstOrDefault(m => m.Index == index);
        trMove?.IsLearned = value;
    }

    private void GiveAll()
    {
        foreach (var trMove in TrMoves)
        {
            trMove.IsLearned = true;
        }

        StateHasChanged();
    }

    private void RemoveAll()
    {
        foreach (var trMove in TrMoves)
        {
            trMove.IsLearned = false;
        }

        StateHasChanged();
    }

    private void Save()
    {
        if (Pokemon is not ITechRecord techRecord)
        {
            return;
        }

        // Apply all TR flag changes to the Pokemon
        foreach (var trMove in TrMoves)
        {
            techRecord.SetMoveRecordFlag(trMove.Index, trMove.IsLearned);
        }

        RefreshService.Refresh();
        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel() =>
        // Close without applying changes - TR flags were not modified
        MudDialog?.Close(DialogResult.Cancel());

    public class TrMoveInfo
    {
        public int Index { get; init; }
        public ushort MoveId { get; init; }
        public string Name { get; init; } = string.Empty;
        public byte Type { get; init; }
        public bool IsLearned { get; set; }
    }
}
