namespace Pkmds.Rcl.Services;

public sealed class DialogOptionsHelper : IDialogOptionsHelper
{
    // Narrow-viewport layout is handled purely in CSS (see app.css — the
    // @media (max-width: 767px) block targeting .mud-dialog). That makes the
    // layout responsive to live window resizes, which setting FullScreen at
    // open-time could not — MudBlazor bakes FullScreen into the dialog once
    // and won't re-react to viewport changes without closing and reopening.
    // Default backdropClick=false: in MudBlazor's default, clicking outside the dialog
    // dismisses it. That's too easy to trigger accidentally — a stray page-background
    // click in the middle of an import flow would discard a parsed-but-not-yet-imported
    // PokePaste team. Opt-in to backdrop dismissal per-dialog by passing backdropClick:true.
    public Task<DialogOptions> BuildAsync(
        MaxWidth desktopMaxWidth,
        bool fullWidth = true,
        bool closeButton = true,
        bool closeOnEscapeKey = true,
        bool backdropClick = false) =>
        Task.FromResult(new DialogOptions
        {
            MaxWidth = desktopMaxWidth,
            FullWidth = fullWidth,
            CloseButton = closeButton,
            CloseOnEscapeKey = closeOnEscapeKey,
            BackdropClick = backdropClick,
        });
}
