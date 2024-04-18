namespace Pkmds.Web.Components;

public partial class DateOnlyPicker
{
    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    [Parameter, EditorRequired]
    public DateOnly? Date { get; set; }

    [Parameter]
    public EventCallback<DateOnly?> DateChanged { get; set; }

    [Parameter, EditorRequired]
    public string? Label { get; set; }

    [Parameter]
    public Expression<Func<DateOnly?>>? For { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? HelperText { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    [Parameter]
    public Color Color { get; set; } = Color.Default;

    private MudDatePicker? datePickerRef;

    private DateTime? DateBindTarget
    {
        get => Date?.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value is not null)
            {
                Date = DateOnly.FromDateTime((DateTime)value);
                DateChanged.InvokeAsync(Date);
            }
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender || For is null)
        {
            return;
        }

        if (EditContext is null)
        {
            throw new Exception("Using 'For' without an 'EditContext' is not supported. Are you missing an 'EditForm'?");
        }

        // Get the private field _fieldidentifier by reflection.
        var fieldIdentifierField = typeof(MudFormComponent<DateTime?, string>).GetField("_fieldIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);

        // Set the field identifier with our DateOnly? expression, avoiding the type issue between DateOnly vs DateTime
        fieldIdentifierField?.SetValue(datePickerRef, FieldIdentifier.Create(For));
    }
}
