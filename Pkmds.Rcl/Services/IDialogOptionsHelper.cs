namespace Pkmds.Rcl.Services;

/// <summary>
/// Centralizes construction of <see cref="DialogOptions"/> so every call site
/// in the app uses the same defaults. Responsive narrow-viewport behavior
/// (full-screen rendering below 768px) is handled entirely in CSS rather
/// than by setting <see cref="DialogOptions.FullScreen"/> here — MudBlazor
/// bakes the value of <c>FullScreen</c> into the dialog at open-time and
/// won't re-react to viewport resizes, so a CSS-driven approach is the only
/// way to stay responsive when the window is resized while a dialog is open.
/// </summary>
public interface IDialogOptionsHelper
{
    /// <summary>
    /// Builds <see cref="DialogOptions"/> sized for the desktop breakpoint
    /// with the project's standard behavior flags (close button, escape key,
    /// backdrop dismissal). The returned options intentionally do not touch
    /// <see cref="DialogOptions.FullScreen"/> — that concern is handled by
    /// the <c>@media (max-width: 767px)</c> block in <c>app.css</c>.
    /// </summary>
    /// <remarks>
    /// The <see cref="Task"/> return type is preserved (rather than a plain
    /// synchronous call) to keep the call-site shape consistent with the
    /// original viewport-aware implementation and to leave room for future
    /// async work (e.g. settings-derived defaults) without churning every
    /// caller.
    /// </remarks>
    Task<DialogOptions> BuildAsync(
        MaxWidth desktopMaxWidth,
        bool fullWidth = true,
        bool closeButton = true,
        bool closeOnEscapeKey = true,
        bool backdropClick = false);
}
