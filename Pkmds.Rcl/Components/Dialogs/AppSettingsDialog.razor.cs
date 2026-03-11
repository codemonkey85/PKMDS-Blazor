namespace Pkmds.Rcl.Components.Dialogs;

public partial class AppSettingsDialog
{
    internal static readonly IReadOnlyList<(LanguageID Id, string Name)> SupportedLanguages =
    [
        (LanguageID.Japanese, "Japanese (日本語)"),
        (LanguageID.English, "English"),
        (LanguageID.French, "French (Français)"),
        (LanguageID.Italian, "Italian (Italiano)"),
        (LanguageID.German, "German (Deutsch)"),
        (LanguageID.Spanish, "Spanish (Español)"),
        (LanguageID.Korean, "Korean (한국어)"),
        (LanguageID.ChineseS, "Chinese Simplified (简体中文)"),
        (LanguageID.ChineseT, "Chinese Traditional (繁體中文)"),
        (LanguageID.SpanishL, "Spanish LATAM (Español LATAM)")
    ];

    private LanguageID _defaultLanguageId = LanguageID.English;
    private string _defaultOtName = string.Empty;
    private uint _defaultSecretId;
    private uint _defaultTrainerId;
    private bool _isHaXEnabled;
    private bool _isVerboseLogging;

    // Working copy — only committed on Save
    private ThemeMode _themeMode;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public AppSettings InitialSettings { get; set; } = new();

    protected override void OnInitialized()
    {
        _themeMode = InitialSettings.ThemeMode switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };
        _isHaXEnabled = InitialSettings.IsHaXEnabled;
        _isVerboseLogging = InitialSettings.IsVerboseLoggingEnabled;
        _defaultOtName = InitialSettings.DefaultOtName;
        _defaultTrainerId = InitialSettings.DefaultTrainerId;
        _defaultSecretId = InitialSettings.DefaultSecretId;
        _defaultLanguageId = InitialSettings.DefaultLanguageId;
    }

    private async Task OnHaXEnabledChanged(bool newValue)
    {
        if (newValue && !InitialSettings.IsHaXEnabled)
        {
            var ack = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "pkmds_hax_warning_ack");
            if (ack != "true")
            {
                await DialogService.ShowMessageBoxAsync(
                    "PKHaX Mode",
                    "Illegal mode activated. Editing restrictions are now lifted. " +
                    "Pokémon created or modified in this mode may be illegal and untradable. " +
                    "Please behave.",
                    "I understand");
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "pkmds_hax_warning_ack", "true");
            }
        }

        _isHaXEnabled = newValue;
    }

    private async Task OnResetAll()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Reset All Settings",
            "This will reset all settings to their default values. Continue?",
            "Reset",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        var defaults = new AppSettings();
        _themeMode = ThemeMode.System;
        _isHaXEnabled = defaults.IsHaXEnabled;
        _isVerboseLogging = defaults.IsVerboseLoggingEnabled;
        _defaultOtName = defaults.DefaultOtName;
        _defaultTrainerId = defaults.DefaultTrainerId;
        _defaultSecretId = defaults.DefaultSecretId;
        _defaultLanguageId = defaults.DefaultLanguageId;
        StateHasChanged();
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private void Save()
    {
        var themeStr = _themeMode switch
        {
            ThemeMode.Light => "light",
            ThemeMode.Dark => "dark",
            _ => "system"
        };

        var updated = new AppSettings
        {
            ThemeMode = themeStr,
            IsHaXEnabled = _isHaXEnabled,
            IsVerboseLoggingEnabled = _isVerboseLogging,
            DefaultOtName = _defaultOtName,
            DefaultTrainerId = _defaultTrainerId,
            DefaultSecretId = _defaultSecretId,
            DefaultLanguageId = _defaultLanguageId
        };

        MudDialog.Close(DialogResult.Ok(updated));
    }

    private enum ThemeMode { Light, System, Dark }
}
