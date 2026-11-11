using System.Linq.Expressions;
using MudBlazor.Extensions;

namespace Pkmds.Rcl.Components;

public partial class AutocompleteSelect<T>
{
    [Parameter] public IEnumerable<T>? Items { get; set; }

    [Parameter] public T? Value { get; set; }

    [Parameter] public EventCallback<T?> ValueChanged { get; set; }

    [Parameter] public Expression<Func<T>>? For { get; set; }

    [Parameter] public string? Label { get; set; }

    [Parameter] public Variant Variant { get; set; } = Variant.Outlined;

    [Parameter] public Margin Margin { get; set; } = Margin.None;

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public bool Clearable { get; set; } = true;

    [Parameter] public bool OpenOnFocus { get; set; } = true;

    [Parameter] public bool CoerceText { get; set; } = true;

    [Parameter] public bool ResetValueOnEmptyText { get; set; } = true;

    // Reopening a selected value should offer alternatives; typing still filters the list.
    [Parameter] public bool Strict { get; set; }

    [Parameter] public bool SelectExactMatchOnTextChange { get; set; }

    // Allow smaller previews, but cap rendering at 10 even if a caller passes null.
    // Unbounded one-letter item searches stalled mobile devices (issue #1122).
    [Parameter] public int? MaxItems { get; set; } = 10;

    [Parameter] public int DebounceInterval { get; set; } = 100;

    [Parameter] public string? Class { get; set; }

    [Parameter] public string? Style { get; set; }

    [Parameter] public Func<T?, string?>? ToStringFunc { get; set; }

    [Parameter] public RenderFragment<T>? ItemTemplate { get; set; }

    [Parameter] public Func<string?, CancellationToken, Task<IEnumerable<T>>>? SearchFunc { get; set; }

    // MudAutocomplete short-circuits null T values in its internal GetItemString to an empty
    // string, which (a) renders dropdown rows as invisible for sentinel values like "Any" and
    // (b) prevents the selected value's text from showing in the input when T is null.
    // Wrapping every value (including null) in a non-null Option record keeps both paths
    // flowing through our ToStringFunc / ItemTemplate unchanged.
    private sealed record Option(T? Value);

    private Option? wrappedValue;
    private MudAutocomplete<Option>? autocomplete;
    private T? previousValue;
    private bool hasParameters;

    protected override void OnParametersSet()
    {
        if (!hasParameters || !EqualityComparer<T>.Default.Equals(Value!, previousValue!))
        {
            wrappedValue = new Option(Value);
        }

        previousValue = Value;
        hasParameters = true;
    }

    private Option? WrappedValue => wrappedValue;

    private IEnumerable<Option> WrappedItems =>
        (Items ?? []).Select(v => new Option(v));

    private Func<Option?, string?> WrappedToStringFunc =>
        opt => opt is null ? string.Empty : EffectiveToStringFunc(opt.Value);

    private RenderFragment<Option> WrappedItemTemplate =>
        opt => EffectiveItemTemplate(opt.Value!);

    private Func<string?, CancellationToken, Task<IEnumerable<Option>>> WrappedSearchFunc =>
        async (query, ct) =>
        {
            var results = await EffectiveSearchFunc(query, ct);
            return results.Select(v => new Option(v));
        };

    private async Task OnWrappedValueChanged(Option? opt)
    {
        var newValue = opt is null ? default : opt.Value;
        var valueChanged = !EqualityComparer<T>.Default.Equals(Value!, newValue!);
        // A cleared input is different from selecting a real default-valued option
        // (e.g. Normal form, Hardy nature, or a wrapped null sentinel such as "Any").
        wrappedValue = opt;
        Value = newValue;
        previousValue = newValue;
        if (valueChanged)
        {
            await ValueChanged.InvokeAsync(newValue);
        }
    }

    private async Task OnClearButtonClick()
    {
        // MudAutocomplete clears its text after raising ValueChanged. Restore the label
        // only after that completes, and only if the reset value is an actual option.
        if (autocomplete is not null && (Items ?? []).Any(item => EqualityComparer<T>.Default.Equals(item, Value!)))
        {
            wrappedValue = new Option(Value);
            await autocomplete.SelectOptionAsync(wrappedValue);
        }
    }

    private void OnOpenChanged(bool open)
    {
        if (!open)
        {
            RestoreClearedOptionLabel();
        }
    }

    private void RestoreClearedOptionLabel()
    {
        // Keyboard deletion must stay empty while typing. Once editing ends, show
        // the reset value if it is a real option. Updating the bound wrapper keeps
        // suggestion clicks intact instead of closing the menu during input blur.
        if (ResetValueOnEmptyText && wrappedValue is null &&
            string.IsNullOrEmpty(autocomplete?.GetState(component => component.Text)) &&
            (Items ?? []).Any(item => EqualityComparer<T>.Default.Equals(item, Value!)))
        {
            wrappedValue = new Option(Value);
        }
    }

    private async Task OnWrappedTextChanged(string? text)
    {
        if (!SelectExactMatchOnTextChange || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var toString = EffectiveToStringFunc;
        T? match = default;
        var foundMatch = false;
        foreach (var item in Items ?? [])
        {
            if (!string.Equals(toString(item), text, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (foundMatch)
            {
                return;
            }

            match = item;
            foundMatch = true;
        }

        if (!foundMatch || wrappedValue is not null && EqualityComparer<T>.Default.Equals(Value!, match!))
        {
            return;
        }

        await OnWrappedValueChanged(new Option(match));
    }

    private Func<T?, string?> EffectiveToStringFunc =>
        ToStringFunc ?? (item => item?.ToString());

    private RenderFragment<T> EffectiveItemTemplate =>
        ItemTemplate ?? (item => builder => builder.AddContent(0, EffectiveToStringFunc(item)));

    private Func<string?, CancellationToken, Task<IEnumerable<T>>> EffectiveSearchFunc =>
        SearchFunc ?? DefaultSearchAsync;

    private Task<IEnumerable<T>> DefaultSearchAsync(string? query, CancellationToken _)
    {
        var source = Items ?? [];

        if (string.IsNullOrWhiteSpace(query))
        {
            // MudAutocomplete needs the full list to locate the selected value. MaxItems
            // bounds the rendered suggestions after centering them on that selection.
            return Task.FromResult(source);
        }

        var toString = EffectiveToStringFunc;
        IEnumerable<T> filtered = source.Where(item =>
            toString(item)?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
        return Task.FromResult(filtered);
    }
}
