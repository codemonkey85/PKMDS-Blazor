namespace Pkmds.Rcl.Components.Dialogs;

public partial class MoveShopDialog
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<MoveShopInfo> MoveShopMoves { get; set; } = [];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadMoveShopMoves();
    }

    private void LoadMoveShopMoves()
    {
        MoveShopMoves.Clear();

        if (Pokemon is not IMoveShop8 moveShop)
        {
            return;
        }

        var moveNames = GameInfo.Strings.movelist;
        var context = Pokemon.Context;

        // Get the list of valid moves from the Permit interface
        var permit = moveShop.Permit;
        var indexes = permit.RecordPermitIndexes;

        for (var i = 0; i < indexes.Length; i++)
        {
            var moveId = indexes[i];
            var moveName = moveNames[moveId];
            var moveType = MoveInfo.GetType(moveId, context);
            var isPurchased = moveShop.GetPurchasedRecordFlag(i);

            MoveShopMoves.Add(new MoveShopInfo
            {
                Index = i,
                MoveId = moveId,
                Name = moveName,
                Type = moveType,
                IsPurchased = isPurchased
            });
        }
    }

    private void ToggleMoveShopFlag(int index, bool value)
    {
        var moveShopMove = MoveShopMoves.FirstOrDefault(m => m.Index == index);
        if (moveShopMove != null)
        {
            moveShopMove.IsPurchased = value;
        }
    }

    private void GiveAll()
    {
        foreach (var moveShopMove in MoveShopMoves)
        {
            moveShopMove.IsPurchased = true;
        }
        StateHasChanged();
    }

    private void RemoveAll()
    {
        foreach (var moveShopMove in MoveShopMoves)
        {
            moveShopMove.IsPurchased = false;
        }
        StateHasChanged();
    }

    private void Save()
    {
        if (Pokemon is not IMoveShop8 moveShop)
        {
            return;
        }

        // Apply all move shop flag changes to the Pokemon
        foreach (var moveShopMove in MoveShopMoves)
        {
            moveShop.SetPurchasedRecordFlag(moveShopMove.Index, moveShopMove.IsPurchased);
        }

        RefreshService.Refresh();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void Cancel()
    {
        // Close without applying changes - move shop flags were not modified
        MudDialog?.Close(DialogResult.Cancel());
    }

    public class MoveShopInfo
    {
        public int Index { get; set; }
        public ushort MoveId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Type { get; set; }
        public bool IsPurchased { get; set; }
    }
}
