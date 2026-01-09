namespace Pkmds.Rcl.Services;

public class LoggingService : ILoggingService
{
    public bool IsVerboseLoggingEnabled
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnLoggingConfigurationChanged?.Invoke(value
                ? LogEventLevel.Debug
                : LogEventLevel.Information);
        }
    }

    public event Action<LogEventLevel>? OnLoggingConfigurationChanged;
}
