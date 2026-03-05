namespace Pkmds.Tests;

/// <summary>
/// Tests for RefreshAwareComponent functionality
/// </summary>
public class RefreshAwareComponentTests
{
    [Fact]
    public void RefreshAwareComponent_SubscribesToAppStateByDefault()
    {
        // Arrange
        var refreshService = new TestRefreshService();
        var component = new TestComponent(refreshService);

        // Act
        component.Initialize();

        // Assert
        refreshService.AppStateSubscriberCount.Should().Be(1);
        refreshService.BoxStateSubscriberCount.Should().Be(0);
        refreshService.PartyStateSubscriberCount.Should().Be(0);
    }

    [Fact]
    public void RefreshAwareComponent_CanSubscribeToMultipleEvents()
    {
        // Arrange
        var refreshService = new TestRefreshService();
        var component = new TestComponentWithMultipleSubscriptions(refreshService);

        // Act
        component.Initialize();

        // Assert
        refreshService.AppStateSubscriberCount.Should().Be(1);
        refreshService.BoxStateSubscriberCount.Should().Be(0);
        refreshService.PartyStateSubscriberCount.Should().Be(1);
    }

    [Fact]
    public void RefreshAwareComponent_UnsubscribesOnDispose()
    {
        // Arrange
        var refreshService = new TestRefreshService();
        var component = new TestComponent(refreshService);
        component.Initialize();

        // Act
        component.Dispose();

        // Assert
        refreshService.AppStateSubscriberCount.Should().Be(0);
    }

    [Fact]
    public void RefreshAwareComponent_CanSubscribeToAllEvents()
    {
        // Arrange
        var refreshService = new TestRefreshService();
        var component = new TestComponentWithAllSubscriptions(refreshService);

        // Act
        component.Initialize();

        // Assert
        refreshService.AppStateSubscriberCount.Should().Be(1);
        refreshService.BoxStateSubscriberCount.Should().Be(1);
        refreshService.PartyStateSubscriberCount.Should().Be(1);
    }

    private class TestComponent : RefreshAwareComponent
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly TestRefreshService testRefreshService;

        public TestComponent(TestRefreshService refreshService)
        {
            testRefreshService = refreshService;
            RefreshServiceField = refreshService;
        }

        public void Initialize() => OnInitialized();
    }

    private class TestComponentWithMultipleSubscriptions : RefreshAwareComponent
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly TestRefreshService testRefreshService;

        public TestComponentWithMultipleSubscriptions(TestRefreshService refreshService)
        {
            testRefreshService = refreshService;
            RefreshServiceField = refreshService;
        }

        protected override RefreshEvents SubscribeTo => RefreshEvents.AppState | RefreshEvents.PartyState;

        public void Initialize() => OnInitialized();
    }

    private class TestComponentWithAllSubscriptions : RefreshAwareComponent
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly TestRefreshService testRefreshService;

        public TestComponentWithAllSubscriptions(TestRefreshService refreshService)
        {
            testRefreshService = refreshService;
            RefreshServiceField = refreshService;
        }

        protected override RefreshEvents SubscribeTo => RefreshEvents.All;

        public void Initialize() => OnInitialized();
    }

    private class TestRefreshService : IRefreshService
    {
        public int AppStateSubscriberCount => _onAppStateChanged?.GetInvocationList().Length ?? 0;
        public int BoxStateSubscriberCount => _onBoxStateChanged?.GetInvocationList().Length ?? 0;
        public int PartyStateSubscriberCount => _onPartyStateChanged?.GetInvocationList().Length ?? 0;

        public event Action? OnAppStateChanged
        {
            add => _onAppStateChanged += value;
            remove => _onAppStateChanged -= value;
        }

        public event Action? OnBoxStateChanged
        {
            add => _onBoxStateChanged += value;
            remove => _onBoxStateChanged -= value;
        }

        public event Action? OnPartyStateChanged
        {
            add => _onPartyStateChanged += value;
            remove => _onPartyStateChanged -= value;
        }

        public event Action? OnUpdateAvailable;
        public event Action<bool>? OnThemeChanged;

        public void Refresh() => _onAppStateChanged?.Invoke();
        public void RefreshBoxState() => _onBoxStateChanged?.Invoke();
        public void RefreshPartyState() => _onPartyStateChanged?.Invoke();

        public void RefreshBoxAndPartyState()
        {
            RefreshBoxState();
            RefreshPartyState();
        }

        public void RefreshTheme(bool isDarkMode) => OnThemeChanged?.Invoke(isDarkMode);
        public void ShowUpdateMessage() => OnUpdateAvailable?.Invoke();

        // ReSharper disable once InconsistentNaming
        private event Action? _onAppStateChanged;

        // ReSharper disable once InconsistentNaming
        private event Action? _onBoxStateChanged;

        // ReSharper disable once InconsistentNaming
        private event Action? _onPartyStateChanged;
    }
}
