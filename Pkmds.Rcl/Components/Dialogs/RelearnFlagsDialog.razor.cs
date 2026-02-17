namespace Pkmds.Rcl.Components.Dialogs;

public partial class RelearnFlagsDialog
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<TrMoveInfo> TrMoves { get; set; } = [];
    private Dictionary<int, bool> OriginalFlags { get; set; } = [];

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
        OriginalFlags.Clear();

        var indexes = permit.RecordPermitIndexes;
        var baseRecordIndex = context == EntityContext.Gen9a ? 1 : 0; // TM001 in Legends: Z-A but is 0-index bits

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
                RecordIndex = recordIndex,
                MoveId = moveId,
                Name = moveName,
                Type = moveType,
                IsLearned = isLearned
            });

            OriginalFlags[recordIndex] = isLearned;
        }
    }

    private void ToggleTrFlag(int recordIndex, bool value)
    {
        var trMove = TrMoves.FirstOrDefault(m => m.RecordIndex == recordIndex);
        if (trMove != null)
        {
            trMove.IsLearned = value;
        }
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

        foreach (var trMove in TrMoves)
        {
            techRecord.SetMoveRecordFlag(trMove.RecordIndex, trMove.IsLearned);
        }

        RefreshService.Refresh();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel()
    {
        // Restore original flags if user cancels
        if (Pokemon is ITechRecord techRecord)
        {
            foreach (var (recordIndex, isLearned) in OriginalFlags)
            {
                techRecord.SetMoveRecordFlag(recordIndex, isLearned);
            }
        }

        MudDialog?.Close(DialogResult.Cancel());
    }

    public class TrMoveInfo
    {
        public int Index { get; set; }
        public int RecordIndex { get; set; }
        public ushort MoveId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Type { get; set; }
        public bool IsLearned { get; set; }
    }
}
