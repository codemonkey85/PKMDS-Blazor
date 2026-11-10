namespace Pkmds.Rcl.Components;

/// <summary>
/// Keeps text-input search and keyboard-free browsing separate on every device.
/// </summary>
public partial class BrowseAutocomplete<T>
{
    private T[] browseItems = [];
    private int browseStart;
    private bool browseOpen;
    private bool browseLoading;
    private bool browseFailed;
    private CancellationTokenSource? browseCancellation;
    private EventCallback<bool> forwardedOpenChanged;
    private EventCallback<bool> browseAwareOpenChanged;
    private EventCallback<MouseEventArgs> forwardedClear;
    private EventCallback<MouseEventArgs> browseAwareClear;
    private ElementReference browseRoot;
    private readonly string browseContentId = $"browse-options-{Guid.NewGuid():N}";
    private IJSObjectReference? browseModule;
    private IJSObjectReference? browseEvents;
    private DotNetObjectReference<BrowseAutocomplete<T>>? browseReference;
    private Task? browseEventsInitialization;
    private bool browseDisposed;

    private int BrowsePageSize => Math.Clamp(MaxItems ?? 10, 1, 10);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (browseOpen && !browseDisposed)
        {
            await (browseEventsInitialization ??= InitializeBrowseEventsAsync());
        }
    }

    private async Task InitializeBrowseEventsAsync()
    {
        browseModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
            "./_content/Pkmds.Rcl/js/browseAutocomplete.js");
        if (browseDisposed)
        {
            return;
        }

        browseReference = DotNetObjectReference.Create(this);
        browseEvents = await browseModule.InvokeAsync<IJSObjectReference>("watch",
            browseRoot, browseContentId, browseReference);
    }

    [JSInvokable]
    public Task DismissBrowseAsync() => InvokeAsync(async () =>
    {
        if (browseDisposed)
        {
            return;
        }

        await CloseBrowseAsync();
        StateHasChanged();
    });

    protected override void OnInitialized()
    {
        base.OnInitialized();
        browseAwareOpenChanged = EventCallback.Factory.Create<bool>(this, OnSearchOpenChangedAsync);
        browseAwareClear = EventCallback.Factory.Create<MouseEventArgs>(this, OnBrowseClearAsync);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // Preserve the parent's close notification (including default-label restoration).
        if (!OpenChanged.Equals(browseAwareOpenChanged))
        {
            forwardedOpenChanged = OpenChanged;
        }

        OpenChanged = browseAwareOpenChanged;
        if (!OnClearButtonClick.Equals(browseAwareClear))
        {
            forwardedClear = OnClearButtonClick;
        }

        OnClearButtonClick = browseAwareClear;
        OnAdornmentClick = EventCallback.Factory.Create<MouseEventArgs>(this, ToggleBrowseAsync);
        AdornmentAriaLabel = $"Browse {Label ?? "dropdown"} options";

        if (GetDisabledState() || GetReadOnlyState())
        {
            CancelBrowse();
        }
    }

    private async Task OnBrowseClearAsync(MouseEventArgs args)
    {
        CancelBrowse();
        await forwardedClear.InvokeAsync(args);
    }

    private async Task OnSearchOpenChangedAsync(bool open)
    {
        if (open)
        {
            // A tap on the text input switches straight back to native typeahead.
            CancelBrowse();
        }

        await forwardedOpenChanged.InvokeAsync(open);
    }

    private async Task ToggleBrowseAsync()
    {
        if (GetDisabledState() || GetReadOnlyState())
        {
            return;
        }

        if (browseOpen)
        {
            await CloseBrowseAsync();
            return;
        }

        // MudAutocomplete's default arrow and list clicks focus the input. Browsing
        // instead blurs it and uses SelectOptionAsync, which does not request focus.
        await BlurAsync();
        await CloseMenuAsync();
        CancelBrowse();
        var cancellation = new CancellationTokenSource();
        browseCancellation = cancellation;
        browseItems = [];
        browseStart = 0;
        browseFailed = false;
        browseLoading = true;
        browseOpen = true;

        try
        {
            var search = SearchFunc?.Invoke(string.Empty, cancellation.Token);
            var results = search is null ? [] : await search;
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            browseItems = results?.ToArray() ?? [];
            var selected = Array.IndexOf(browseItems, ReadValue!);
            browseStart = Math.Clamp(selected - BrowsePageSize / 2, 0,
                Math.Max(0, browseItems.Length - BrowsePageSize));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Typing, closing, or leaving this editor superseded the browse request.
        }
        catch (Exception exception)
        {
            if (!cancellation.IsCancellationRequested)
            {
                browseFailed = true;
                Logger.LogWarning(exception, "Could not load dropdown browse options");
            }
        }
        finally
        {
            if (ReferenceEquals(browseCancellation, cancellation))
            {
                browseLoading = false;
            }
        }
    }

    private async Task SelectBrowseOptionAsync(T item)
    {
        CancelBrowse();
        await SelectOptionAsync(item);
        if (browseEvents is not null)
        {
            await browseEvents.InvokeVoidAsync("focusArrow");
        }
    }

    private async Task CloseBrowseAsync()
    {
        var wasOpen = browseOpen;
        CancelBrowse();
        if (wasOpen)
        {
            await forwardedOpenChanged.InvokeAsync(false);
        }
    }

    private void CancelBrowse()
    {
        browseOpen = false;
        browseItems = [];
        browseStart = 0;
        browseLoading = false;
        browseFailed = false;
        browseCancellation?.Cancel();
        browseCancellation?.Dispose();
        browseCancellation = null;
    }

    private void PreviousBrowsePage() => browseStart = Math.Max(0, browseStart - BrowsePageSize);

    private void NextBrowsePage() => browseStart = Math.Min(browseStart + BrowsePageSize, browseItems.Length - 1);

    private Task OnBrowseKeyDown(KeyboardEventArgs args) => args.Key == "Escape" ? CloseBrowseAsync() : Task.CompletedTask;

    protected override async ValueTask DisposeAsyncCore()
    {
        browseDisposed = true;
        CancelBrowse();
        if (browseEventsInitialization is not null)
        {
            await browseEventsInitialization;
        }

        if (browseEvents is not null)
        {
            await browseEvents.InvokeVoidAsync("dispose");
            await browseEvents.DisposeAsync();
        }

        browseReference?.Dispose();
        if (browseModule is not null)
        {
            await browseModule.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
