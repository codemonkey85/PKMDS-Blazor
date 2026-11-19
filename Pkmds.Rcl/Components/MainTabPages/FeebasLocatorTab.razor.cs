using Pkmds.Core.Feebas;

namespace Pkmds.Rcl.Components.MainTabPages;

public partial class FeebasLocatorTab
{
    private const string Route119Url = "_content/Pkmds.Rcl/maps/feebas/route119.png";
    private const string MtCoronetUrl = "_content/Pkmds.Rcl/maps/feebas/mtcoronet.png";

    private const int Route119Width = 641;
    private const int Route119Height = 1553;
    private const int MtCoronetWidth = 512;
    private const int MtCoronetHeight = 518;

    private SaveFile? saveFile;
    private ushort[]? tiles;
    private uint seed;
    private string seedHex = string.Empty;
    private string seedFormat = "X4";
    private string editedSeedHex = string.Empty;
    private int seedMaxLength = 4;
    private string locationLabel = string.Empty;
    private string mapUrl = string.Empty;
    private int mapWidth;
    private int mapHeight;
    private List<TileMarker> markers = [];
    private bool fitToView = true;
    private bool showNumbers;

    private void ToggleFitToView() => fitToView = !fitToView;

    private void ToggleShowNumbers() => showNumbers = !showNumbers;

    private bool TryParseEditedSeed(out uint value) =>
        uint.TryParse(editedSeedHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

    private bool IsSeedDifferentFromSaved =>
        TryParseEditedSeed(out var parsed) && parsed != seed;

    private bool CanApplySeed =>
        TryParseEditedSeed(out _) && IsSeedDifferentFromSaved;

    private void OnSeedInputChanged()
    {
        if (saveFile is null || !TryParseEditedSeed(out var parsed))
        {
            return;
        }

        var newTiles = saveFile.Generation == 3
            ? Feebas3.GetTiles(parsed)
            : Feebas4.GetTiles(parsed);
        tiles = newTiles;
        markers = BuildMarkers(saveFile, newTiles);
    }

    private void RandomizeSeed()
    {
        if (saveFile is null)
        {
            return;
        }

        var newSeed = saveFile.Generation == 3
            ? (uint)Util.Rand.Next(0x10000)
            : Util.Rand.Rand32();
        editedSeedHex = newSeed.ToString(seedFormat, CultureInfo.InvariantCulture);
        OnSeedInputChanged();
    }

    private void ApplySeed()
    {
        if (saveFile is null || !TryParseEditedSeed(out var parsed))
        {
            return;
        }

        if (!FeebasSeedAccessor.TryWriteSeed(saveFile, parsed))
        {
            Snackbar.Add("Could not write Feebas seed: unsupported save type.", Severity.Error);
            return;
        }

        seed = parsed;
        seedHex = seed.ToString(seedFormat, CultureInfo.InvariantCulture);
        editedSeedHex = seedHex;
        Snackbar.Add($"Feebas seed updated to 0x{seedHex}.", Severity.Success);
        RefreshService.Refresh();
    }

    private void ResetSeed()
    {
        editedSeedHex = seedHex;
        OnSeedInputChanged();
    }

    private const string BaseContainerStyle =
        "border: 1px solid var(--mud-palette-lines-default); " +
        "border-radius: 4px; " +
        "background: var(--mud-palette-background-grey); " +
        "padding: 4px;";

    private string ContainerStyle => fitToView
        ? $"{BaseContainerStyle} text-align: center;"
        : $"{BaseContainerStyle} overflow-x: auto; max-width: 100%;";

    private string WrapperStyle => fitToView
        ? "position: relative; display: inline-block;"
        : $"position: relative; display: inline-block; width: {mapWidth}px; height: {mapHeight}px;";

    private string ImageStyle => fitToView
        ? "display: block; max-width: 100%; max-height: 50vh; width: auto; height: auto; image-rendering: pixelated;"
        : "display: block; width: 100%; height: 100%; image-rendering: pixelated;";

    private string MarkerStyle(TileMarker marker)
    {
        var leftPct = (double)marker.X / mapWidth * 100;
        var topPct = (double)marker.Y / mapHeight * 100;
        var widthPct = (double)marker.Width / mapWidth * 100;
        var heightPct = (double)marker.Height / mapHeight * 100;
        return string.Create(CultureInfo.InvariantCulture,
            $"position: absolute; left: {leftPct:F4}%; top: {topPct:F4}%; width: {widthPct:F4}%; height: {heightPct:F4}%; background: rgba(244, 67, 54, 0.7); border: 1px solid rgba(244, 67, 54, 1); pointer-events: none; box-sizing: border-box;");
    }

    private const string BadgeBaseStyle =
        "display: inline-flex; " +
        "align-items: center; " +
        "justify-content: center; " +
        "width: 22px; " +
        "height: 22px; " +
        "border-radius: 50%; " +
        "background: #ffffff; " +
        "border: 2px solid #d32f2f; " +
        "color: #d32f2f; " +
        "font-size: 12px; " +
        "font-weight: bold; " +
        "box-sizing: border-box; " +
        "line-height: 1; " +
        "flex-shrink: 0;";

    private const string BadgeListStyle = BadgeBaseStyle + " margin: 0 4px 0 8px; vertical-align: middle;";

    private string MarkerLabelStyle(TileMarker marker)
    {
        var centerX = (marker.X + marker.Width / 2.0) / mapWidth * 100;
        var centerY = (marker.Y + marker.Height / 2.0) / mapHeight * 100;
        return string.Create(CultureInfo.InvariantCulture,
            $"{BadgeBaseStyle} position: absolute; left: {centerX:F4}%; top: {centerY:F4}%; transform: translate(-50%, -50%); pointer-events: none; z-index: 2;");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadFromAppState();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadFromAppState();
    }

    private void LoadFromAppState()
    {
        saveFile = AppState?.SaveFile;
        if (saveFile is null || !FeebasSeedAccessor.IsSupported(saveFile))
        {
            tiles = null;
            markers = [];
            return;
        }

        var seedValue = FeebasSeedAccessor.TryReadSeed(saveFile);
        if (seedValue is null)
        {
            tiles = null;
            markers = [];
            return;
        }

        seed = seedValue.Value;
        tiles = FeebasSeedAccessor.GetTiles(saveFile);

        if (saveFile.Generation == 3)
        {
            seedFormat = "X4";
            seedMaxLength = 4;
            locationLabel = "Route 119 — Hoenn";
            mapUrl = Route119Url;
            mapWidth = Route119Width;
            mapHeight = Route119Height;
        }
        else
        {
            seedFormat = "X8";
            seedMaxLength = 8;
            locationLabel = "Mt. Coronet B1F";
            mapUrl = MtCoronetUrl;
            mapWidth = MtCoronetWidth;
            mapHeight = MtCoronetHeight;
        }

        seedHex = seed.ToString(seedFormat, CultureInfo.InvariantCulture);
        editedSeedHex = seedHex;
        markers = BuildMarkers(saveFile, tiles);
    }

    private static List<TileMarker> BuildMarkers(SaveFile sav, ushort[]? tiles)
    {
        var result = new List<TileMarker>();
        if (tiles is null)
        {
            return result;
        }

        if (sav.Generation == 3)
        {
            for (var idx = 0; idx < tiles.Length; idx++)
            {
                var tile = tiles[idx];
                if (Feebas3.IsUnderBridge(tile))
                {
                    var underBridgeRows = FeebasTileCoordinates.Route119TilesUnderBridge.GetLength(0);
                    for (var i = 0; i < underBridgeRows; i++)
                    {
                        result.Add(new TileMarker(
                            FeebasTileCoordinates.Route119TilesUnderBridge[i, 0],
                            FeebasTileCoordinates.Route119TilesUnderBridge[i, 1],
                            FeebasTileCoordinates.Gen3MarkerSize,
                            FeebasTileCoordinates.Gen3MarkerSize,
                            idx,
                            i == 0));
                    }
                    continue;
                }

                if (Feebas3.IsAccessible(tile))
                {
                    result.Add(new TileMarker(
                        FeebasTileCoordinates.Route119Tiles[tile - 4, 0],
                        FeebasTileCoordinates.Route119Tiles[tile - 4, 1],
                        FeebasTileCoordinates.Gen3MarkerSize,
                        FeebasTileCoordinates.Gen3MarkerSize,
                        idx,
                        true));
                }
            }
        }
        else
        {
            for (var idx = 0; idx < tiles.Length; idx++)
            {
                var tile = tiles[idx];
                if (tile >= FeebasTileCoordinates.MtCoronetTiles.GetLength(0))
                {
                    continue;
                }

                result.Add(new TileMarker(
                    FeebasTileCoordinates.MtCoronetTiles[tile, 0],
                    FeebasTileCoordinates.MtCoronetTiles[tile, 1],
                    FeebasTileCoordinates.Gen4MarkerWidth,
                    FeebasTileCoordinates.Gen4MarkerHeight,
                    idx,
                    true));
            }
        }

        return result;
    }

    private readonly record struct TileMarker(int X, int Y, int Width, int Height, int Index, bool ShowLabel);
}
