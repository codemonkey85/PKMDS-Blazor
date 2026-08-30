using System.Linq.Expressions;

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

    [Parameter] public bool Strict { get; set; } = true;

    [Parameter] public bool SelectExactMatchOnTextChange { get; set; }

    // Keep MudAutocomplete's own bounded default. Leaving this null overrides MudBlazor's
    // default of 10 and renders every match, which made one-letter item searches stall for
    // several seconds on mobile devices (issue #1122).
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

    private Option WrappedValue => new(Value);

    private IEnumerable<Option> WrappedItems =>
        (Items ?? []).Select(v => new Option(v));

    private Func<Option?, string?> WrappedToStringFunc =>
        opt => EffectiveToStringFunc(opt is null ? default : opt.Value);

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
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
    }

    private async Task OnWrappedTextChanged(string? text)
    {
        if (!SelectExactMatchOnTextChange || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var matches = (Items ?? [])
            .Where(item => string.Equals(EffectiveToStringFunc(item), text, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();
        if (matches.Count != 1 || EqualityComparer<T>.Default.Equals(Value!, matches[0]))
        {
            return;
        }

        await OnWrappedValueChanged(new Option(matches[0]));
    }

    private Func<T?, string?> EffectiveToStringFunc =>
        ToStringFunc ?? (item => item?.ToString());

    private RenderFragment<T> EffectiveItemTemplate =>
        ItemTemplate ?? (item => builder => builder.AddContent(0, EffectiveToStringFunc(item)));

    private Func<string?, CancellationToken, Task<IEnumerable<T>>> EffectiveSearchFunc =>
        SearchFunc ?? DefaultSearchAsync;

    // How many items to preview in the dropdown when the user hasn't typed anything yet.
    // Keeps large source lists (moves, items, species) from rendering hundreds of rows on
    // focus, while letting small lists (balls, natures, types) show all their items.
    private const int EmptyQueryPreview = 30;

    private Task<IEnumerable<T>> DefaultSearchAsync(string? query, CancellationToken _)
    {
        var source = Items ?? [];

        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IEnumerable<T>>(source.Take(EmptyQueryPreview).ToList());
        }

        var toString = EffectiveToStringFunc;
        IEnumerable<T> filtered = source.Where(item =>
            toString(item)?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
        return Task.FromResult(filtered);
    }
}
