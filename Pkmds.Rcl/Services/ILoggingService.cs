namespace Pkmds.Rcl.Services;

/// <summary>
/// Service for controlling application logging levels.
/// Allows users to toggle verbose logging for debugging purposes.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Gets or sets whether verbose logging is enabled.
    /// When true, sets log level to Verbose; when false, sets to Information.
    /// </summary>
    bool IsVerboseLoggingEnabled { get; set; }

    /// <summary>
    /// Event triggered when logging configuration changes.
    /// Provides the new log event level.
    /// </summary>
    event Action<LogEventLevel>? OnLoggingConfigurationChanged;
}
