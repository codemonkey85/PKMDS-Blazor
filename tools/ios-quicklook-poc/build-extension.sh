#!/usr/bin/env bash
# Builds the C# Quick Look preview + thumbnail extensions (.appex) and the SwiftUI host app,
# then embeds both .appex bundles into the host app's PlugIns/. Defaults to the iOS Simulator
# (no signing required); use --device for an AOT'd ios-arm64 build.
#
#   ./build-extension.sh                          # iOS Simulator (Mono, no AOT)
#   ./build-extension.sh --device                 # ios-arm64 (NativeAOT) — host app produced unsigned, deploy via Xcode
#   SIM_NAME="iPhone 15 Pro" ./build-extension.sh
set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/../.." && pwd )"

# Ad-hoc signs every Mach-O inside an .appex (main binary + every embedded
# .dylib), then signs the bundle itself. iOS extensions on the simulator
# are amfid-validated at load time and SIGKILL on any unsigned page.
sign_appex() {
    local appex="$1"
    while IFS= read -r dylib; do
        codesign --force --sign - --timestamp=none "$dylib" >/dev/null 2>&1
    done < <(find "$appex" -type f -name '*.dylib')
    codesign --force --sign - --timestamp=none "$appex" >/dev/null 2>&1
}

TARGET_DEVICE=0
if [[ "${1:-}" == "--device" ]]; then
    TARGET_DEVICE=1
fi

# Both paths build a .appex with Microsoft.iOS.Sdk + IsAppExtension=true. They
# diverge on RID + AOT: simulator uses iossimulator-arm64 + Mono via `dotnet
# build` (Microsoft.iOS.Sdk's Publish target rejects simulator RIDs); device
# uses ios-arm64 + NativeAOT via `dotnet publish`.
CSPROJ="$SCRIPT_DIR/PkmdsQuickLook/PkmdsQuickLook.csproj"
THUMB_CSPROJ="$SCRIPT_DIR/PkmdsQuickLookThumbnail/PkmdsQuickLookThumbnail.csproj"
if [[ "$TARGET_DEVICE" -eq 1 ]]; then
    DOTNET_CMD=(publish -c Release -r ios-arm64)
    DOTNET_OUT_DIR="$SCRIPT_DIR/PkmdsQuickLook/bin/Release/net10.0-ios/ios-arm64"
    THUMB_OUT_DIR="$SCRIPT_DIR/PkmdsQuickLookThumbnail/bin/Release/net10.0-ios/ios-arm64"
    XCODE_DEST="generic/platform=iOS"
    XCODE_PRODUCTS_DIR="Release-iphoneos"
    XCODE_CONFIG="Release"
else
    DOTNET_CMD=(build -c Debug -r iossimulator-arm64)
    DOTNET_OUT_DIR="$SCRIPT_DIR/PkmdsQuickLook/bin/Debug/net10.0-ios/iossimulator-arm64"
    THUMB_OUT_DIR="$SCRIPT_DIR/PkmdsQuickLookThumbnail/bin/Debug/net10.0-ios/iossimulator-arm64"
    XCODE_PRODUCTS_DIR="Debug-iphonesimulator"
    XCODE_CONFIG="Debug"
fi

APPEX_SRC="$DOTNET_OUT_DIR/PkmdsQuickLook.appex"
THUMB_APPEX_SRC="$THUMB_OUT_DIR/PkmdsQuickLookThumbnail.appex"

XCODE_DIR="$SCRIPT_DIR/xcode"
PROJECT="$XCODE_DIR/PkmdsHost.xcodeproj"
DERIVED="$XCODE_DIR/build"
APP_PATH="$DERIVED/Build/Products/$XCODE_PRODUCTS_DIR/PkmdsHost.app"

# Verify the ios .NET workload (Microsoft.iOS.Sdk) is installed.
if ! dotnet workload list 2>/dev/null | grep -qE '^ios\b'; then
    cat <<'EOF' >&2
==> error: the 'ios' .NET workload is not installed.
    Install it with:
        sudo dotnet workload install ios
EOF
    exit 1
fi

echo "==> dotnet ${DOTNET_CMD[*]} (preview extension)"
dotnet "${DOTNET_CMD[@]}" "$CSPROJ" --nologo

[[ -d "$APPEX_SRC" ]] || { echo "missing .appex at $APPEX_SRC" >&2; exit 1; }

echo "==> dotnet ${DOTNET_CMD[*]} (thumbnail extension)"
dotnet "${DOTNET_CMD[@]}" "$THUMB_CSPROJ" --nologo

[[ -d "$THUMB_APPEX_SRC" ]] || { echo "missing thumbnail .appex at $THUMB_APPEX_SRC" >&2; exit 1; }

echo "==> xcodegen generate"
( cd "$XCODE_DIR" && xcodegen generate --quiet )

if [[ "$TARGET_DEVICE" -eq 1 ]]; then
    echo "==> xcodebuild PkmdsHost (Release, iOS device, unsigned)"
    xcodebuild \
        -project "$PROJECT" \
        -scheme PkmdsHost \
        -configuration "$XCODE_CONFIG" \
        -destination "$XCODE_DEST" \
        -derivedDataPath "$DERIVED" \
        CODE_SIGNING_ALLOWED=NO \
        -quiet \
        build

    [[ -d "$APP_PATH" ]] || { echo "missing host app at $APP_PATH" >&2; exit 1; }

    echo "==> embed .appex bundles into host app"
    mkdir -p "$APP_PATH/PlugIns"
    rm -rf "$APP_PATH/PlugIns/PkmdsQuickLook.appex"
    cp -R "$APPEX_SRC" "$APP_PATH/PlugIns/"
    rm -rf "$APP_PATH/PlugIns/PkmdsQuickLookThumbnail.appex"
    cp -R "$THUMB_APPEX_SRC" "$APP_PATH/PlugIns/"

    # Copy bundled sprites into the thumbnail extension.
    echo "==> bundle sprites into thumbnail extension"
    SPRITES_SRC="$REPO_ROOT/Pkmds.Rcl/wwwroot/sprites"
    SPRITES_DST="$APP_PATH/PlugIns/PkmdsQuickLookThumbnail.appex/sprites"
    rm -rf "$SPRITES_DST"
    mkdir -p "$SPRITES_DST"
    cp -r "$SPRITES_SRC/a"  "$SPRITES_DST/"
    cp -r "$SPRITES_SRC/ai" "$SPRITES_DST/"
    cp -r "$SPRITES_SRC/bi" "$SPRITES_DST/"

    # Even on the simulator, iOS extensions must carry a valid code signature —
    # the host app gets a pass without one, but amfid SIGKILLs unsigned .appex
    # bundles on launch ("Code Signature Invalid" / dyld __LINKEDIT page fault).
    # Ad-hoc signing covers the simulator path; real-device deployment needs a
    # full Developer ID via Xcode.
    echo "==> ad-hoc sign embedded .appex bundles"
    sign_appex "$APP_PATH/PlugIns/PkmdsQuickLook.appex"
    sign_appex "$APP_PATH/PlugIns/PkmdsQuickLookThumbnail.appex"

    echo
    echo "Built unsigned host app with embedded extensions: $APP_PATH"
    echo "Deploy to a real device by opening the project in Xcode, setting your"
    echo "Team / signing identity, and running on a connected device."
    exit 0
fi

# Simulator path — pick a simulator (override with SIM_NAME=...)
SIM_NAME="${SIM_NAME:-}"
if [[ -n "$SIM_NAME" ]]; then
    SIM_LINE=$(xcrun simctl list devices available 2>/dev/null \
        | grep -E "^\s+$SIM_NAME \(" \
        | head -1)
else
    SIM_LINE=$(xcrun simctl list devices available 2>/dev/null \
        | grep -E "^\s+iPhone " \
        | head -1)
fi
SIM_UUID=$(echo "$SIM_LINE" | sed -E 's/.*\(([A-F0-9-]+)\).*/\1/')
SIM_NAME=$(echo "$SIM_LINE" | sed -E 's/^\s+(.*) \([A-F0-9-]+\).*/\1/')

if [[ -z "$SIM_UUID" ]]; then
    echo "no iPhone simulator found. Available:" >&2
    xcrun simctl list devices available | grep -E "^\s+iPhone|^\s+iPad" | sed -E 's/^\s+//' >&2
    exit 1
fi

echo "==> using simulator $SIM_NAME ($SIM_UUID)"
xcrun simctl boot "$SIM_UUID" 2>/dev/null || true
open -a Simulator

echo "==> xcodebuild PkmdsHost (Debug, iOS Simulator, unsigned)"
xcodebuild \
    -project "$PROJECT" \
    -scheme PkmdsHost \
    -configuration "$XCODE_CONFIG" \
    -destination "id=$SIM_UUID" \
    -derivedDataPath "$DERIVED" \
    CODE_SIGNING_ALLOWED=NO \
    -quiet \
    build

[[ -d "$APP_PATH" ]] || { echo "missing host app at $APP_PATH" >&2; exit 1; }

echo "==> embed .appex bundles into host app"
mkdir -p "$APP_PATH/PlugIns"
rm -rf "$APP_PATH/PlugIns/PkmdsQuickLook.appex"
cp -R "$APPEX_SRC" "$APP_PATH/PlugIns/"
rm -rf "$APP_PATH/PlugIns/PkmdsQuickLookThumbnail.appex"
cp -R "$THUMB_APPEX_SRC" "$APP_PATH/PlugIns/"

echo "==> bundle sprites into thumbnail extension"
SPRITES_SRC="$REPO_ROOT/Pkmds.Rcl/wwwroot/sprites"
SPRITES_DST="$APP_PATH/PlugIns/PkmdsQuickLookThumbnail.appex/sprites"
rm -rf "$SPRITES_DST"
mkdir -p "$SPRITES_DST"
cp -r "$SPRITES_SRC/a"  "$SPRITES_DST/"
cp -r "$SPRITES_SRC/ai" "$SPRITES_DST/"
cp -r "$SPRITES_SRC/bi" "$SPRITES_DST/"

echo "==> ad-hoc sign embedded .appex bundles"
sign_appex "$APP_PATH/PlugIns/PkmdsQuickLook.appex"
sign_appex "$APP_PATH/PlugIns/PkmdsQuickLookThumbnail.appex"

echo "==> install + launch"
xcrun simctl uninstall "$SIM_UUID" com.bondcodes.pkmds.host.ios 2>/dev/null || true
xcrun simctl install "$SIM_UUID" "$APP_PATH"
xcrun simctl launch "$SIM_UUID" com.bondcodes.pkmds.host.ios

echo
echo "Launched PkmdsHost on $SIM_NAME."
echo "Drag a .pk*/.sav from Finder onto the Simulator window, then:"
echo "  - Long-press a file in Files.app → Quick Look → HTML preview."
echo "  - Switch to grid/column view with large icons → thumbnail extension renders trainer cards + sprites."
