namespace Pkmds.Rcl.Theming;

public static class AppTheme
{
    /// <summary>
    /// Tailwind-derived color ramp — the single source of truth for the app's palette,
    /// including values mirrored into static host assets that can't reference C#
    /// (index.html, manifest.webmanifest, the pre-boot splash styles in app.css, and
    /// BrowserNotSupported.html). <c>ThemeSyncTests</c> verifies those files stay in sync.
    /// </summary>
    public static class Colors
    {
        public const string White = "#FFFFFF";
        public const string Blue400 = "#60A5FA";
        public const string Blue800 = "#1E40AF";
        public const string Green400 = "#4ADE80";
        public const string Green600 = "#16A34A";
        public const string Violet400 = "#A78BFA";
        public const string Violet600 = "#7C3AED";
        public const string Stone50 = "#FAFAF9";
        public const string Stone100 = "#F5F5F4";
        public const string Stone200 = "#E7E5E4";
        public const string Stone400 = "#A8A29E";
        public const string Stone600 = "#57534E";
        public const string Stone700 = "#44403C";
        public const string Stone800 = "#292524";
        public const string Stone900 = "#1C1917";
        public const string Stone950 = "#0C0A09";
    }

    /// <summary>
    /// Window-chrome color advertised to the OS via the theme-color meta tag in index.html
    /// and theme_color in manifest.webmanifest. A single static value only works because
    /// AppbarBackground is identical in both palettes — ThemeSyncTests enforces that
    /// invariant, so if the palettes ever diverge the theme-color plumbing must become
    /// mode-aware at the same time.
    /// </summary>
    public const string PwaThemeColor = Colors.Stone900;

    /// <summary>
    /// PWA manifest background_color — the color shown behind the pre-boot splash screen.
    /// </summary>
    public const string PwaBackgroundColor = Colors.Stone50;

    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue800,
            Secondary = Colors.Green600,
            Tertiary = Colors.Violet600,
            Background = Colors.Stone50,
            BackgroundGray = Colors.Stone100,
            Surface = Colors.White,
            AppbarBackground = PwaThemeColor,
            AppbarText = Colors.Stone50,
            DrawerBackground = Colors.White,
            DrawerText = Colors.Stone900,
            DrawerIcon = Colors.Stone700,
            TextPrimary = Colors.Stone900,
            TextSecondary = Colors.Stone600,
            ActionDefault = Colors.Stone600,
            Divider = Colors.Stone200,
            LinesDefault = Colors.Stone200,
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue400,
            Secondary = Colors.Green400,
            Tertiary = Colors.Violet400,
            Background = Colors.Stone950,
            BackgroundGray = Colors.Stone900,
            Surface = Colors.Stone800,
            AppbarBackground = PwaThemeColor,
            AppbarText = Colors.Stone50,
            DrawerBackground = Colors.Stone950,
            DrawerText = Colors.Stone50,
            DrawerIcon = Colors.Stone400,
            TextPrimary = Colors.Stone50,
            TextSecondary = Colors.Stone400,
            ActionDefault = Colors.Stone400,
            Divider = Colors.Stone600,
            LinesDefault = Colors.Stone600,
        },
    };
}
