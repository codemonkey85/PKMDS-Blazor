namespace Pkmds.Rcl.Theming;

public static class AppTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1E40AF",
            Secondary = "#16A34A",
            Tertiary = "#7C3AED",
            Background = "#FAFAF9",
            BackgroundGray = "#F5F5F4",
            Surface = "#FFFFFF",
            AppbarBackground = "#1C1917",
            AppbarText = "#FAFAF9",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1C1917",
            DrawerIcon = "#44403C",
            TextPrimary = "#1C1917",
            TextSecondary = "#57534E",
            ActionDefault = "#57534E",
            Divider = "#E7E5E4",
            LinesDefault = "#E7E5E4",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#60A5FA",
            Secondary = "#4ADE80",
            Tertiary = "#A78BFA",
            Background = "#0C0A09",
            BackgroundGray = "#1C1917",
            Surface = "#292524",
            AppbarBackground = "#1C1917",
            AppbarText = "#FAFAF9",
            DrawerBackground = "#0C0A09",
            DrawerText = "#FAFAF9",
            DrawerIcon = "#A8A29E",
            TextPrimary = "#FAFAF9",
            TextSecondary = "#A8A29E",
            ActionDefault = "#A8A29E",
            Divider = "#44403C",
            LinesDefault = "#44403C",
        },
    };
}
