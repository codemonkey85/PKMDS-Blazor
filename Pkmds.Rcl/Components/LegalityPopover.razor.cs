namespace Pkmds.Rcl.Components;

public partial class LegalityPopover
{
    private bool open;

    /// <summary>
    /// The failing <see cref="CheckResult"/> to describe. When null, the popover renders
    /// nothing (legal fields stay visually quiet).
    /// </summary>
    [Parameter]
    public CheckResult? Result { get; set; }

    /// <summary>
    /// The legality analysis the <see cref="Result"/> came from. Required to humanize
    /// the message via <see cref="LegalityLocalizationContext"/>.
    /// </summary>
    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    /// <summary>
    /// Override for the severity. Used by the <see cref="MoveResult"/> overload where the
    /// severity doesn't come from a <see cref="CheckResult"/>.
    /// </summary>
    [Parameter]
    public PKHexSeverity? SeverityOverride { get; set; }

    /// <summary>
    /// Override for the humanized body text. Used by the <see cref="MoveResult"/> overload
    /// or any caller that wants to supply its own message.
    /// </summary>
    [Parameter]
    public string? MessageOverride { get; set; }

    /// <summary>
    /// Short label identifying the field, e.g. "PID", "Nickname". Combined with severity
    /// to form the popover header (e.g. "[Invalid] PID"). When null, falls back to the
    /// identifier derived from <see cref="Result"/>.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Optional quick-fix handler. When bound, a fix button is rendered in the popover body.
    /// </summary>
    [Parameter]
    public EventCallback OnQuickFix { get; set; }

    /// <summary>Label for the quick-fix button. Defaults to "Quick fix".</summary>
    [Parameter]
    public string QuickFixLabel { get; set; } = "Quick fix";

    private PKHexSeverity EffectiveSeverity =>
        SeverityOverride ?? Result?.Judgement ?? PKHexSeverity.Valid;

    private string Message =>
        MessageOverride ?? LegalityHelpers.Humanize(Analysis, Result);

    private string PopoverTitle
    {
        get
        {
            var label = Title ?? (Result is { } r
                ? LegalityHelpers.GetIdentifierLabel(r.Identifier)
                : string.Empty);
            var severity = LegalityHelpers.GetSeverityLabel(EffectiveSeverity);
            return string.IsNullOrEmpty(label)
                ? $"[{severity}]"
                : $"[{severity}] {label}";
        }
    }

    // Hide the popover entirely for Valid severity (nothing to show).
    private bool IsVisible => EffectiveSeverity != PKHexSeverity.Valid;

    protected override void OnParametersSet()
    {
        // Reset open state when the field becomes Valid, so a later transition
        // back to Invalid doesn't re-show the popover already-open.
        if (EffectiveSeverity == PKHexSeverity.Valid)
        {
            open = false;
        }
    }

    private string GetIcon() => EffectiveSeverity switch
    {
        PKHexSeverity.Fishy => Icons.Material.Filled.Warning,
        _ => Icons.Material.Filled.Cancel
    };

    private Color GetColor() => EffectiveSeverity switch
    {
        PKHexSeverity.Fishy => Color.Warning,
        _ => Color.Error
    };

    private void Toggle() => open = !open;

    private void Close() => open = false;

    private async Task HandleQuickFix()
    {
        Close();
        await OnQuickFix.InvokeAsync();
    }
}
