#!/usr/bin/env bash
# Builds the AOT dylib + Xcode host app + Quick Look extensions (preview + thumbnail), signs
# ad-hoc, bundles sprites, registers with Launch Services / PluginKit, and smoke-tests both.
#
#   ./build-extension.sh                                  # default fixture
#   ./build-extension.sh path/to/file.pk5
#
set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/../.." && pwd )"
RID="osx-arm64"

FIXTURE="${1:-$REPO_ROOT/TestFiles/Lucario_B06DDFAD.pk5}"
# .gci is a non-prohibited extension mapped to com.bondcodes.pkmds.save-file; qlmanage uses it
# for the thumbnail smoke test because qlmanage cannot dispatch to sandboxed extensions via .sav.
GCI_FIXTURE="$REPO_ROOT/TestFiles/Test-Save-Moon.gci"

CSPROJ="$SCRIPT_DIR/PkmdsNative/PkmdsNative.csproj"
PUBLISH_DIR="$SCRIPT_DIR/PkmdsNative/bin/Release/net10.0/$RID/publish"
DYLIB_SRC="$PUBLISH_DIR/PkmdsNative.dylib"
DYLIB_DEST="$SCRIPT_DIR/build-resources/PkmdsNative.dylib"

XCODE_DIR="$SCRIPT_DIR/xcode"
PROJECT="$XCODE_DIR/PkmdsQuickLook.xcodeproj"
DERIVED="$SCRIPT_DIR/xcode/build"
APP_PATH="$DERIVED/Build/Products/Release/PkmdsHost.app"

PREVIEW_APPEX="$APP_PATH/Contents/PlugIns/PkmdsQuickLook.appex"
THUMBNAIL_APPEX="$APP_PATH/Contents/PlugIns/PkmdsQuickLookThumbnail.appex"
SPOTLIGHT_APPEX="$APP_PATH/Contents/Resources/PkmdsSpotlight.mdimporter"

PREVIEW_ENTITLEMENTS="$XCODE_DIR/PkmdsQuickLook/PkmdsQuickLook.entitlements"
THUMBNAIL_ENTITLEMENTS="$XCODE_DIR/PkmdsQuickLookThumbnail/PkmdsQuickLookThumbnail.entitlements"

echo "==> dotnet publish ($RID, AOT)"
dotnet publish "$CSPROJ" -c Release -r "$RID" --nologo

[[ -f "$DYLIB_SRC" ]] || { echo "missing $DYLIB_SRC" >&2; exit 1; }

echo "==> stage dylib at $DYLIB_DEST"
cp "$DYLIB_SRC" "$DYLIB_DEST"

echo "==> xcodegen generate"
( cd "$XCODE_DIR" && xcodegen generate --quiet )

echo "==> xcodebuild PkmdsHost (Release, ad-hoc signed)"
xcodebuild \
    -project "$PROJECT" \
    -scheme PkmdsHost \
    -configuration Release \
    -derivedDataPath "$DERIVED" \
    CODE_SIGN_IDENTITY=- \
    CODE_SIGN_STYLE=Manual \
    DEVELOPMENT_TEAM= \
    -quiet \
    build

[[ -d "$APP_PATH" ]] || { echo "missing built app at $APP_PATH" >&2; exit 1; }

# ── Bundle sprites into the thumbnail extension ─────────────────────────────────────────────────
# Sprites are NOT checked into the repo (they live in Pkmds.Rcl/wwwroot/sprites/).
# Copy the three categories the thumbnail provider needs after xcodebuild and before re-signing.
echo "==> bundle sprites into thumbnail extension"
SPRITES_SRC="$REPO_ROOT/Pkmds.Rcl/wwwroot/sprites"
SPRITES_DST="$THUMBNAIL_APPEX/Contents/Resources/sprites"
rm -rf "$SPRITES_DST"
mkdir -p "$SPRITES_DST"
cp -r "$SPRITES_SRC/a"  "$SPRITES_DST/"
cp -r "$SPRITES_SRC/ai" "$SPRITES_DST/"
cp -r "$SPRITES_SRC/bi" "$SPRITES_DST/"

# ── Sign ────────────────────────────────────────────────────────────────────────────────────────
# Sign inner → outer (dylib → appex → host app). The sprite copy above invalidates the thumbnail
# appex signature that xcodebuild already applied, so we must re-sign it here.
echo "==> codesign extensions and host app (ad-hoc)"
codesign --force --sign - --timestamp=none \
    "$PREVIEW_APPEX/Contents/Frameworks/PkmdsNative.dylib"
codesign --force --sign - --timestamp=none --options runtime \
    --entitlements "$PREVIEW_ENTITLEMENTS" "$PREVIEW_APPEX"

codesign --force --sign - --timestamp=none \
    "$THUMBNAIL_APPEX/Contents/Frameworks/PkmdsNative.dylib"
codesign --force --sign - --timestamp=none --options runtime \
    --entitlements "$THUMBNAIL_ENTITLEMENTS" "$THUMBNAIL_APPEX"

codesign --force --sign - --timestamp=none "$APP_PATH"

# ── Deploy Spotlight MDImporter ─────────────────────────────────────────────────────────────────
# The MDImporter teaches Spotlight to assign com.bondcodes.pkmds.save-file to .sav/.dat/.fla files.
# Once indexed, Finder dispatches our Quick Look extension instead of treating them as opaque data.
# We install into ~/Library/Spotlight (user-level) rather than /Library/Spotlight to avoid sudo.
echo "==> install Spotlight MDImporter"
SPOTLIGHT_DST="$HOME/Library/Spotlight/PkmdsSpotlight.mdimporter"
rm -rf "$SPOTLIGHT_DST"
cp -R "$SPOTLIGHT_APPEX" "$SPOTLIGHT_DST"
codesign --force --sign - --timestamp=none "$SPOTLIGHT_DST"
# Reload the importer and re-index any PKHeX save files already on disk.
mdimport -r "$SPOTLIGHT_DST" 2>/dev/null || true

# ── Deploy ──────────────────────────────────────────────────────────────────────────────────────
echo "==> deploy to /Applications and register only that copy"
LSREGISTER=/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister
INSTALLED=/Applications/PkmdsHost.app

osascript -e 'quit app "PkmdsHost"' 2>/dev/null || true
sleep 1

# Unregister all known stale copies before installing. Xcode DerivedData and iOS/simulator POC
# builds can linger in the LS database with old UTI declarations (e.g. prohibited .sav/.dat/.fla
# extensions), causing ThumbnailsAgent to block the thumbnail extension at startup. Unregister
# proactively to avoid this; lsregister -u is a no-op for paths that aren't registered.
"$LSREGISTER" -u "$APP_PATH"  2>/dev/null || true
"$LSREGISTER" -u "$INSTALLED" 2>/dev/null || true
for STALE_PATH in \
    "$SCRIPT_DIR/../embedded-host-ios-poc/xcode/build/Build/Products/Debug-iphonesimulator/PkmdsHost.app" \
    "$SCRIPT_DIR/../ios-quicklook-poc/xcode/build/Build/Products/Debug-iphonesimulator/PkmdsHost.app" \
    "$HOME/Library/Developer/Xcode/DerivedData/PkmdsQuickLook-"*/Build/Products/Release/PkmdsHost.app
do
    "$LSREGISTER" -u "$STALE_PATH" 2>/dev/null || true
done
rm -rf "$INSTALLED"
cp -R "$APP_PATH" "$INSTALLED"
"$LSREGISTER" -f "$INSTALLED"

# Register both extensions with PluginKit; the thumbnail provider needs separate registration
# because com.apple.quicklook.preview and com.apple.quicklook.thumbnail are different plug-in
# point databases that lsregister alone does not always update.
pluginkit -a "$INSTALLED/Contents/PlugIns/PkmdsQuickLook.appex"
pluginkit -e use -i com.bondcodes.pkmds.host.quicklook
pluginkit -a "$INSTALLED/Contents/PlugIns/PkmdsQuickLookThumbnail.appex"
pluginkit -e use -i com.bondcodes.pkmds.host.quicklook.thumbnail

# Kill quicklookd and ThumbnailsAgent so they restart with the clean LS registration.
# ThumbnailsAgent caches extension-to-UTI mappings at startup; it must restart after
# the LS database is updated to pick up the corrected UTI declarations.
killall pkd quicklookd 2>/dev/null || true
killall -9 "com.apple.quicklook.ThumbnailsAgent" 2>/dev/null || true
sleep 1
qlmanage -r >/dev/null 2>&1 || true
qlmanage -r cache >/dev/null 2>&1 || true

# ── Smoke tests ─────────────────────────────────────────────────────────────────────────────────
# Re-index test .sav files so Finder picks up the new MDImporter metadata immediately.
echo "==> mdimport test save files"
find "$REPO_ROOT/TestFiles" -maxdepth 1 \( -name "*.sav" -o -name "*.dat" -o -name "*.fla" \) \
    -exec mdimport {} \; 2>/dev/null || true
sleep 1
# qlmanage -p opens an interactive window; run it with a 10-second timeout so the script doesn't
# block waiting for the user to close the window.
echo "==> qlmanage -p (preview): $FIXTURE"
# `timeout` is GNU coreutils and not available on stock macOS; use a background-kill workaround.
( qlmanage -p "$FIXTURE" 2>&1 & QLP=$! ; sleep 10 ; kill "$QLP" 2>/dev/null ; true ) | tail -10 || true

echo "==> qlmanage -t (thumbnail, 256px): $FIXTURE"
( qlmanage -t -s 256 -o /tmp "$FIXTURE" 2>&1 & QLT=$! ; sleep 15 ; kill "$QLT" 2>/dev/null ; true ) | tail -10 || true
if [[ -n "$(ls /tmp/"$(basename "$FIXTURE")"*.png 2>/dev/null)" ]]; then
    echo "    thumbnail PNG written to /tmp"
fi

# Save-file thumbnail smoke test uses .gci because qlmanage cannot dispatch to sandboxed
# extensions for prohibited extensions like .sav. In Finder, .sav files need a Spotlight
# MDImporter to assign the com.bondcodes.pkmds.save-file UTI; .gci/.dsv/.srm work automatically.
if [[ ! -f "$GCI_FIXTURE" ]]; then
    SRC_SAV="$REPO_ROOT/TestFiles/moon.sav"
    if [[ -f "$SRC_SAV" ]]; then
        cp "$SRC_SAV" "$GCI_FIXTURE"
        mdimport "$GCI_FIXTURE" 2>/dev/null || true
        echo "    created $GCI_FIXTURE for save thumbnail test"
    fi
fi
if [[ -f "$GCI_FIXTURE" ]]; then
    echo "==> qlmanage -t (thumbnail, 256px): $(basename "$GCI_FIXTURE")"
    ( qlmanage -t -s 256 -o /tmp "$GCI_FIXTURE" 2>&1 & QLT=$! ; sleep 15 ; kill "$QLT" 2>/dev/null ; true ) | tail -10 || true
fi

echo
echo "Built and installed: $INSTALLED"
echo "Press Space on a .pk*/.gci/.sav file in Finder to preview/thumbnail."
echo "Thumbnail icons appear in Finder's icon or gallery view (may need qlmanage -r cache)."
echo ".sav/.dat/.fla files are re-indexed by mdimport automatically; new files need 'mdimport <file>' or a Spotlight index pass."
