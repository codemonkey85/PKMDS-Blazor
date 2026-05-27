namespace Pkmds.Preview;

/// <summary>
/// Single source of truth for the file extensions the PKMDS previewers handle, shared by
/// the macOS Quick Look, iOS Quick Look, and Windows Shell preview-handler PoCs.
///
/// <para>
/// The Windows preview handler reads <see cref="Extensions"/> directly when registering its
/// shell extension. The macOS/iOS bundles must declare the same extensions statically in
/// their host <c>Info.plist</c> <c>UTTypeTagSpecification</c> arrays (the OS requires the
/// types to be declared in the bundle, so they can't read this list at runtime). The three
/// groups below map 1:1 to the three declared UTTypes — keep the plists in sync with them.
/// </para>
/// </summary>
public static class PreviewFileTypes
{
    /// <summary>Pokémon entity files — UTType <c>com.bondcodes.pkmds.pkm-file</c>.</summary>
    public static readonly string[] PkmEntities =
    [
        ".pk1", ".pk2", ".pk3", ".pk4", ".pk5", ".pk6", ".pk7", ".pk8", ".pk9",
        ".pa8", ".pa9", ".pb7", ".pb8",
        ".sk2", ".ck3", ".xk3", ".bk4", ".rk4",
    ];

    /// <summary>Save files — UTType <c>com.bondcodes.pkmds.save-file</c>.</summary>
    public static readonly string[] SaveFiles =
    [
        ".sav", ".dat", ".bin", ".gci", ".dsv", ".srm", ".fla",
    ];

    /// <summary>Wonder cards / mystery gifts — UTType <c>com.bondcodes.pkmds.wonder-card</c>.</summary>
    public static readonly string[] WonderCards =
    [
        ".pgt", ".pcd", ".wc3", ".wc4", ".pgf",
        ".wc5full", ".wc6", ".wc6full", ".wc7", ".wc7full",
        ".wr7", ".wb7", ".wb7full",
        ".wc8", ".wc8full", ".wb8", ".wa8",
        ".wc9", ".wa9",
    ];

    /// <summary>All supported extensions, with a leading dot and lower-cased.</summary>
    public static readonly string[] Extensions =
        [.. PkmEntities, .. SaveFiles, .. WonderCards];
}
