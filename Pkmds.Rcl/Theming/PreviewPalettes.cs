namespace Pkmds.Rcl.Theming;

public enum PreviewPalette
{
    Default,
    Pokedex,
    Workshop,
    Dawn,
}

public static class PreviewPalettes
{
    public static MudTheme? Resolve(PreviewPalette palette) => palette switch
    {
        PreviewPalette.Pokedex => Pokedex,
        PreviewPalette.Workshop => Workshop,
        PreviewPalette.Dawn => Dawn,
        _ => null,
    };

    public static string DisplayName(PreviewPalette palette) => palette switch
    {
        PreviewPalette.Default => "Default (current)",
        PreviewPalette.Pokedex => "Pokédex (blue + green)",
        PreviewPalette.Workshop => "Workshop (teal + indigo)",
        PreviewPalette.Dawn => "Dawn (indigo + cyan)",
        _ => palette.ToString(),
    };

    public static MudTheme Pokedex { get; } = new()
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
            Surface = "#1C1917",
            AppbarBackground = "#1C1917",
            AppbarText = "#FAFAF9",
            DrawerBackground = "#0C0A09",
            DrawerText = "#FAFAF9",
            DrawerIcon = "#A8A29E",
            TextPrimary = "#FAFAF9",
            TextSecondary = "#A8A29E",
            ActionDefault = "#A8A29E",
            Divider = "#292524",
            LinesDefault = "#292524",
        },
    };

    public static MudTheme Workshop { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0D9488",
            Secondary = "#6366F1",
            Tertiary = "#8B5CF6",
            Background = "#F8FAFC",
            BackgroundGray = "#F1F5F9",
            Surface = "#FFFFFF",
            AppbarBackground = "#0F172A",
            AppbarText = "#F1F5F9",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#0F172A",
            DrawerIcon = "#475569",
            TextPrimary = "#0F172A",
            TextSecondary = "#475569",
            ActionDefault = "#475569",
            Divider = "#E2E8F0",
            LinesDefault = "#E2E8F0",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2DD4BF",
            Secondary = "#818CF8",
            Tertiary = "#A78BFA",
            Background = "#0F172A",
            BackgroundGray = "#1E293B",
            Surface = "#1E293B",
            AppbarBackground = "#0F172A",
            AppbarText = "#F1F5F9",
            DrawerBackground = "#0F172A",
            DrawerText = "#F1F5F9",
            DrawerIcon = "#94A3B8",
            TextPrimary = "#F1F5F9",
            TextSecondary = "#94A3B8",
            ActionDefault = "#94A3B8",
            Divider = "#334155",
            LinesDefault = "#334155",
        },
    };

    public static MudTheme Dawn { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#4F46E5",
            Secondary = "#06B6D4",
            Tertiary = "#F43F5E",
            Background = "#FAFAFA",
            BackgroundGray = "#F4F4F5",
            Surface = "#FFFFFF",
            AppbarBackground = "#18181B",
            AppbarText = "#FAFAFA",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#18181B",
            DrawerIcon = "#52525B",
            TextPrimary = "#18181B",
            TextSecondary = "#52525B",
            ActionDefault = "#52525B",
            Divider = "#E4E4E7",
            LinesDefault = "#E4E4E7",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#818CF8",
            Secondary = "#22D3EE",
            Tertiary = "#FB7185",
            Background = "#09090B",
            BackgroundGray = "#18181B",
            Surface = "#18181B",
            AppbarBackground = "#09090B",
            AppbarText = "#FAFAFA",
            DrawerBackground = "#09090B",
            DrawerText = "#FAFAFA",
            DrawerIcon = "#A1A1AA",
            TextPrimary = "#FAFAFA",
            TextSecondary = "#A1A1AA",
            ActionDefault = "#A1A1AA",
            Divider = "#27272A",
            LinesDefault = "#27272A",
        },
    };
}
