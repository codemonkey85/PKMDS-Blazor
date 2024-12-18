namespace Pkmds.Web.Components;

public partial class GenderDisplayComponent
{
    [Parameter] public Gender Gender { get; set; }

    [Parameter] public EventCallback<Gender> OnChange { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public bool IncludeGenderless { get; set; }

    private static string GetGenderIcon(Gender gender) => gender switch
    {
        Gender.Male => Icons.Material.Filled.Male,
        Gender.Female => Icons.Material.Filled.Female,
        Gender.Genderless => Icons.Material.Filled.Block,
        _ => string.Empty,
    };

    private static string GetGenderColor(Gender gender) => gender switch
    {
        Gender.Male => Colors.Blue.Default,
        Gender.Female => Colors.Red.Default,
        _ => string.Empty,
    };

    private RenderFragment GenderDisplayIcon(Gender gender) => !Disabled && OnChange.HasDelegate
        ? GenderButton(gender, IncludeGenderless)
        : GenderIconOnly(gender);

    private static RenderFragment GenderDisplayAscii(Gender gender) =>
        GenderText(gender.ToString(), GetGenderColor(gender));
}
