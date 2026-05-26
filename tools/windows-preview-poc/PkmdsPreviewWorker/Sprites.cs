namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Points the shared <c>HtmlRenderer</c> at the sprite set bundled next to the worker, so previews
/// embed sprites as <c>data:</c> URIs and make <em>zero web requests</em> (the PokeAPI URLs remain
/// only as a fallback if a bundled file is missing). Sprite files are copied to
/// <c>&lt;output&gt;/sprites/</c> by the project file.
/// </summary>
internal static class Sprites
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "sprites");

    public static void UseBundled() => HtmlRenderer.SpriteResolver = ResolveDataUri;

    private static string? ResolveDataUri(string relativePath)
    {
        try
        {
            var path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                return null;
            return "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }
        catch
        {
            return null;
        }
    }
}
