namespace Pkmds.Rcl.Components;

/// <summary>
/// Base component that automatically subscribes to refresh events.
/// Reduces boilerplate code for components that need to refresh on state changes.
/// </summary>
public abstract class RefreshAwareComponent : ComponentBase, IDisposable
{
    // Note: Services are injected via _Imports.razor, not here
    // This avoids property hiding conflicts
    [Inject]
    private IRefreshService? _refreshServiceField { get; set; }

    private IRefreshService RefreshServiceInternal => _refreshServiceField!;

    /// <summary>
    /// Override this property to control which refresh events trigger StateHasChanged.
    /// By default, only OnAppStateChanged is subscribed.
    /// </summary>
    protected virtual RefreshEvents SubscribeTo => RefreshEvents.AppState;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (SubscribeTo.HasFlag(RefreshEvents.AppState))
        {
            RefreshServiceInternal.OnAppStateChanged += StateHasChanged;
        }

        if (SubscribeTo.HasFlag(RefreshEvents.BoxState))
        {
            RefreshServiceInternal.OnBoxStateChanged += StateHasChanged;
        }

        if (SubscribeTo.HasFlag(RefreshEvents.PartyState))
        {
            RefreshServiceInternal.OnPartyStateChanged += StateHasChanged;
        }
    }

    public void Dispose()
    {
        if (SubscribeTo.HasFlag(RefreshEvents.AppState))
        {
            RefreshServiceInternal.OnAppStateChanged -= StateHasChanged;
        }

        if (SubscribeTo.HasFlag(RefreshEvents.BoxState))
        {
            RefreshServiceInternal.OnBoxStateChanged -= StateHasChanged;
        }

        if (SubscribeTo.HasFlag(RefreshEvents.PartyState))
        {
            RefreshServiceInternal.OnPartyStateChanged -= StateHasChanged;
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override this method to add custom disposal logic.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}

/// <summary>
/// Flags enum to control which refresh events a component subscribes to.
/// </summary>
[Flags]
public enum RefreshEvents
{
    None = 0,
    AppState = 1 << 0,
    BoxState = 1 << 1,
    PartyState = 1 << 2,
    BoxAndParty = BoxState | PartyState,
    All = AppState | BoxState | PartyState
}
