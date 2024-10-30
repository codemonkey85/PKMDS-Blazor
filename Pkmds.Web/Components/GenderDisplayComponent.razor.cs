namespace Pkmds.Web.Components;
public partial class GenderDisplayComponent
{
    [Parameter]
    public Gender Gender { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    public RenderFragment GenderDisplayIcon(Gender gender)
    {
        var icon = gender switch
        {
            Gender.Male => Icons.Material.Filled.Male,
            Gender.Female => Icons.Material.Filled.Female,
            Gender.Genderless => Icons.Material.Filled.Block,
            _ => string.Empty,
        };
        var color = GetGenderColor(gender);

        return !Disabled && OnClick.HasDelegate
            ? GenderButton(gender, icon, color)
            : GenderIconOnly(icon, color);
    }

    public static RenderFragment GenderDisplayAscii(Gender gender)
    {
        var text = gender switch
        {
            Gender.Male => "Male",
            Gender.Female => "Female",
            Gender.Genderless => "Genderless",
            _ => string.Empty,
        };
        var color = GetGenderColor(gender);
        return GenderText(text, color);
    }

    private static string GetGenderColor(Gender gender) => gender switch
    {
        Gender.Male => Colors.Blue.Default,
        Gender.Female => Colors.Red.Default,
        _ => string.Empty,
    };
}
