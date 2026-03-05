namespace Pkmds.Rcl.Components;

/// <summary>
/// Base component that automatically subscribes to refresh events.
/// Reduces boilerplate code for components that need to refresh on state changes.
/// </summary>
public abstract class RefreshAwareComponent : ComponentBase, IDisposable
{
    // Cache the subscription flags to avoid evaluating SubscribeTo during disposal
    private RefreshEvents subscribedEvents = RefreshEvents.None;

    // Note: Services are injected via _Imports.razor, not here
    // This avoids property hiding conflicts
    [Inject]
    protected IRefreshService? RefreshServiceField { get; set; }

    private IRefreshService RefreshServiceInternal => RefreshServiceField!;

    /// <summary>
    /// Override this property to control which refresh events trigger StateHasChanged.
    /// By default, only OnAppStateChanged is subscribed.
    /// </summary>
    protected virtual RefreshEvents SubscribeTo => RefreshEvents.AppState;

    public void Dispose()
    {
        // Use the cached subscription flags instead of evaluating SubscribeTo
        if (subscribedEvents.HasFlag(RefreshEvents.AppState))
        {
            RefreshServiceInternal.OnAppStateChanged -= StateHasChanged;
        }

        if (subscribedEvents.HasFlag(RefreshEvents.BoxState))
        {
            RefreshServiceInternal.OnBoxStateChanged -= StateHasChanged;
        }

        if (subscribedEvents.HasFlag(RefreshEvents.PartyState))
        {
            RefreshServiceInternal.OnPartyStateChanged -= StateHasChanged;
        }

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Cache the events we're subscribing to
        subscribedEvents = SubscribeTo;

        if (subscribedEvents.HasFlag(RefreshEvents.AppState))
        {
            RefreshServiceInternal.OnAppStateChanged += StateHasChanged;
        }

        if (subscribedEvents.HasFlag(RefreshEvents.BoxState))
        {
            RefreshServiceInternal.OnBoxStateChanged += StateHasChanged;
        }

        if (subscribedEvents.HasFlag(RefreshEvents.PartyState))
        {
            RefreshServiceInternal.OnPartyStateChanged += StateHasChanged;
        }
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
