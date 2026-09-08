namespace Pkmds.Rcl.Components;

public partial class TabErrorBoundary
{
    private ErrorBoundary? errorBoundary;

    [Parameter]
    [EditorRequired]
    public required string TabName { get; set; }

    [Parameter]
    [EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    private void Recover() => errorBoundary?.Recover();
}
