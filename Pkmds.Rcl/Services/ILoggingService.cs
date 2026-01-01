namespace Pkmds.Rcl.Services;

public interface ILoggingService
{
    bool IsVerboseLoggingEnabled { get; set; }

    event Action? OnLoggingConfigurationChanged;
}
