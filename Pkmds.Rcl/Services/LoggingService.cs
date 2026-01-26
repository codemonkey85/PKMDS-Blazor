namespace Pkmds.Rcl.Services;

/// <summary>
/// Implementation of logging service for controlling application log levels.
/// Allows runtime toggling between Information and Debug log levels.
/// </summary>
public class LoggingService : ILoggingService
{
    /// <inheritdoc />
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
            // Notify subscribers of the new log level
            OnLoggingConfigurationChanged?.Invoke(value
                ? LogEventLevel.Debug
                : LogEventLevel.Information);
        }
    }

    /// <inheritdoc />
    public event Action<LogEventLevel>? OnLoggingConfigurationChanged;
}
