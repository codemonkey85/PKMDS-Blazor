namespace Pkmds.Rcl.Components.Dialogs;

public partial class PlusFlagsDialog
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<PlusMoveInfo> PlusMoves { get; } = [];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadPlusMoves();
    }

    private void LoadPlusMoves()
    {
        PlusMoves.Clear();

        if (Pokemon is not PA9 pa9)
        {
            return;
        }

        var moveNames = GameInfo.Strings.movelist;
        var context = Pokemon.Context;

        // Get the list of plus moves for PA9 (Legends Z-A)
        var plusMovesList = PersonalInfo9ZA.PlusMoves;

        for (var i = 0; i < plusMovesList.Length; i++)
        {
            var moveId = plusMovesList[i];
            var moveName = moveNames[moveId];
            var moveType = MoveInfo.GetType(moveId, context);
            var isPlus = pa9.GetMovePlusFlag(i);

            PlusMoves.Add(new PlusMoveInfo
            {
                Index = i,
                MoveId = moveId,
                Name = moveName,
                Type = moveType,
                IsPlus = isPlus
            });
        }
    }

    private void TogglePlusFlag(int index, bool value)
    {
        var plusMove = PlusMoves.FirstOrDefault(m => m.Index == index);
        if (plusMove != null)
        {
            plusMove.IsPlus = value;
        }
    }

    private void GiveAll()
    {
        foreach (var plusMove in PlusMoves)
        {
            plusMove.IsPlus = true;
        }

        StateHasChanged();
    }

    private void RemoveAll()
    {
        foreach (var plusMove in PlusMoves)
        {
            plusMove.IsPlus = false;
        }

        StateHasChanged();
    }

    private void Save()
    {
        if (Pokemon is not PA9 pa9)
        {
            return;
        }

        // Apply all plus flag changes to the Pokemon
        foreach (var plusMove in PlusMoves)
        {
            pa9.SetMovePlusFlag(plusMove.Index, plusMove.IsPlus);
        }

        RefreshService.Refresh();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel() =>
        // Close without applying changes - plus flags were not modified
        MudDialog?.Close(DialogResult.Cancel());

    public class PlusMoveInfo
    {
        public int Index { get; set; }
        public ushort MoveId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Type { get; set; }
        public bool IsPlus { get; set; }
    }
}
