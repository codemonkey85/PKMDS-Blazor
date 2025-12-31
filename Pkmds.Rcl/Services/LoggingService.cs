namespace Pkmds.Rcl.Services;

public class LoggingService : ILoggingService
{
    private bool _isVerboseLoggingEnabled;

    public bool IsVerboseLoggingEnabled
    {
        get => _isVerboseLoggingEnabled;
        set
        {
            if (_isVerboseLoggingEnabled != value)
            {
                _isVerboseLoggingEnabled = value;
                OnLoggingConfigurationChanged?.Invoke();
            }
        }
    }

    public event Action? OnLoggingConfigurationChanged;
}
