namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class RibbonsTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public LegalityAnalysis? Analysis { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    /// <summary>
    /// Returns the worst-severity legality check result for a given mark property name,
    /// or null if the mark is valid or is a regular ribbon (not a mark).
    /// Only marks have per-entry <see cref="CheckResult" />s with <see cref="CheckResult.Argument" />
    /// set to the <see cref="RibbonIndex" />. Regular ribbon checks are grouped into a single result
    /// with Argument = count, so per-ribbon targeting is not possible for them.
    /// </summary>
    private CheckResult? GetRibbonCheckResult(string propertyName)
    {
        if (Analysis is not { } la)
        {
            return null;
        }

        // Regular ribbon results are grouped (Argument = count of invalid ribbons, not a RibbonIndex).
        // Only mark results are per-entry with Argument = RibbonIndex.
        if (!IsMarkEntry(propertyName))
        {
            return null;
        }

        // "RibbonMarkCurry" → short name "MarkCurry" → RibbonIndex.MarkCurry
        var shortName = propertyName.StartsWith("Ribbon", StringComparison.Ordinal)
            ? propertyName["Ribbon".Length..]
            : propertyName;

        if (!Enum.TryParse<RibbonIndex>(shortName, true, out var idx))
        {
            return null;
        }

        CheckResult? worst = null;
        foreach (var r in la.Results)
        {
            if (r.Valid)
            {
                continue;
            }

            if (r.Identifier is not CheckIdentifier.RibbonMark)
            {
                continue;
            }

            if ((RibbonIndex)r.Argument != idx)
            {
                continue;
            }

            if (worst is null || r.Judgement > worst.Value.Judgement)
            {
                worst = r;
            }
        }

        return worst;
    }

    private IEnumerable<(string DisplayName, CheckResult Result)> GetInvalidRibbonResults()
    {
        if (Analysis is not { } la)
        {
            yield break;
        }

        foreach (var r in la.Results)
        {
            if (r.Valid)
            {
                continue;
            }

            if (r.Identifier == CheckIdentifier.Ribbon)
            {
                // Grouped result — Argument is the count of invalid ribbons, not a RibbonIndex.
                // The humanized message already enumerates the ribbon names; use empty display name
                // so the template doesn't duplicate "Ribbons:" alongside "Invalid Ribbons:".
                yield return (string.Empty, r);
            }
            else if (r.Identifier == CheckIdentifier.RibbonMark)
            {
                // Per-mark result — Argument is the RibbonIndex of the specific mark.
                var propertyName = "Ribbon" + (RibbonIndex)r.Argument;
                yield return (GetRibbonDisplayName(propertyName), r);
            }
        }
    }

    private string HumanizeRibbonCheckResult(CheckResult result)
    {
        if (Analysis is not { } la)
        {
            return string.Empty;
        }

        var ctx = LegalityLocalizationContext.Create(la);
        return ctx.Humanize(in result);
    }

    /// <summary>
    /// Returns the message portion of a humanized check result, stripping the leading "Severity: "
    /// prefix. Used in the Ribbon Issues list where the icon already communicates severity.
    /// </summary>
    private string GetRibbonIssueMessage(CheckResult result)
    {
        var humanized = HumanizeRibbonCheckResult(result);
        var colonIdx = humanized.IndexOf(": ", StringComparison.Ordinal);
        return colonIdx >= 0
            ? humanized[(colonIdx + 2)..]
            : humanized;
    }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private static string GetRibbonDisplayName(string propertyName) =>
        RibbonHelper.GetRibbonDisplayName(propertyName);

    private static string GetRibbonSprite(RibbonInfo info) =>
        RibbonHelper.GetRibbonSprite(info);

    private static bool IsMarkEntry(string name) =>
        RibbonHelper.IsMarkEntry(name);

    private List<RibbonInfo> GetAllRibbonInfo() =>
        RibbonHelper.GetAllRibbonInfo(Pokemon);

    private void ToggleRibbon(string propertyName)
    {
        if (Pokemon is null)
        {
            return;
        }

        var current = ReflectUtil.GetValue(Pokemon, propertyName) is bool and true;
        ReflectUtil.SetValue(Pokemon, propertyName, !current);
        StateHasChanged();
    }

    private void SetRibbonCount(string propertyName, int count)
    {
        if (Pokemon is null)
        {
            return;
        }

        // Clamp the incoming count to a valid range before casting to byte.
        var maxAllowed = (int)byte.MaxValue;
        foreach (var ribbon in GetAllRibbonInfo())
        {
            if (ribbon.Name != propertyName)
            {
                continue;
            }

            if (ribbon.MaxCount < maxAllowed)
            {
                maxAllowed = ribbon.MaxCount;
            }

            break;
        }

        var clamped = count;
        if (clamped < 0)
        {
            clamped = 0;
        }

        if (clamped > maxAllowed)
        {
            clamped = maxAllowed;
        }

        ReflectUtil.SetValue(Pokemon, propertyName, (byte)clamped);
        StateHasChanged();
    }

    private void GiveAllRibbons()
    {
        if (Pokemon is null)
        {
            return;
        }

        foreach (var ribbon in GetAllRibbonInfo())
        {
            var value = ribbon.Type is RibbonValueType.Boolean
                ? (object)true
                : (byte)ribbon.MaxCount;
            ReflectUtil.SetValue(Pokemon, ribbon.Name, value);
        }

        StateHasChanged();
    }

    private void ClearAllRibbons()
    {
        if (Pokemon is null)
        {
            return;
        }

        foreach (var ribbon in GetAllRibbonInfo())
        {
            var value = ribbon.Type is RibbonValueType.Boolean
                ? (object)false
                : (byte)0;
            ReflectUtil.SetValue(Pokemon, ribbon.Name, value);
        }

        StateHasChanged();
    }
}
