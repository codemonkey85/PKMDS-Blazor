namespace Pkmds.Rcl.Components;

public partial class BoxComponent : IDisposable
{
    [Parameter]
    public int BoxId { get; set; }

    private BoxEdit? BoxEdit { get; set; }

    private Guid subscriptionId;

    private bool previewRow = true;

    protected override void OnParametersSet()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        AppState.SelectedPokemon = null;
        AppState.Refresh();

        BoxEdit = new BoxEdit(AppState.SaveFile);
        BoxEdit.LoadBox(BoxId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var subscriptionResult = await BreakpointListener.Subscribe(
                HandleBreakpoint,
                new ResizeOptions
                {
                    ReportRate = 250,
                    NotifyOnBreakpointOnly = false,
                });
            subscriptionId = subscriptionResult.SubscriptionId;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void HandleBreakpoint(MudBlazor.Breakpoint breakpoint)
    {
        if (breakpoint switch
        {
            MudBlazor.Breakpoint.Xs or
            MudBlazor.Breakpoint.Sm => false,
            _ => true,
        } != previewRow)
        {
            previewRow = !previewRow;
            StateHasChanged();
        }
    }

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose()
    {
        AppState.OnAppStateChanged -= StateHasChanged;
        BreakpointListener.Unsubscribe(subscriptionId);
    }
}
