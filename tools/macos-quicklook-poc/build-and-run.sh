#!/usr/bin/env bash
# Builds the AOT dylib and Swift driver, then runs against PKM and save fixtures.
#
#   ./build-and-run.sh                                       # default fixtures
#   ./build-and-run.sh path/to/file.pk9                      # custom pkm fixture
#   ./build-and-run.sh path/to/file.pk9 path/to/file.sav     # custom both
#   ./build-and-run.sh "" "" osx-arm64                       # override RID, default fixtures
set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/../.." && pwd )"

PKM_FIXTURE="${1:-$REPO_ROOT/TestFiles/Lucario_B06DDFAD.pk5}"
SAV_FIXTURE="${2:-$REPO_ROOT/TestFiles/Test-Save-Scarlet.sav}"
RID="${3:-osx-arm64}"

CSPROJ="$SCRIPT_DIR/PkmdsNative/PkmdsNative.csproj"
PUBLISH_DIR="$SCRIPT_DIR/PkmdsNative/bin/Release/net10.0/$RID/publish"
DYLIB="$PUBLISH_DIR/PkmdsNative.dylib"

echo "==> dotnet publish ($RID, AOT)"
dotnet publish "$CSPROJ" -c Release -r "$RID" --nologo

if [[ ! -f "$DYLIB" ]]; then
    echo "expected dylib not found at $DYLIB" >&2
    exit 1
fi

ls -lh "$DYLIB"

echo "==> swiftc swift-cli/main.swift"
SWIFT_BIN="$SCRIPT_DIR/swift-cli/pkmds-poc"
swiftc -O -o "$SWIFT_BIN" "$SCRIPT_DIR/swift-cli/main.swift"

if [[ -n "$PKM_FIXTURE" && -f "$PKM_FIXTURE" ]]; then
    echo "==> pkm json: $PKM_FIXTURE"
    "$SWIFT_BIN" "$DYLIB" pkm "$PKM_FIXTURE"
    echo "==> pkm html: $PKM_FIXTURE -> /tmp/pkmds-poc-pkm.html"
    "$SWIFT_BIN" "$DYLIB" pkm-html "$PKM_FIXTURE" > /tmp/pkmds-poc-pkm.html
    echo "==> file-html (pkm): $PKM_FIXTURE -> /tmp/pkmds-poc-file-pkm.html"
    "$SWIFT_BIN" "$DYLIB" file-html "$PKM_FIXTURE" > /tmp/pkmds-poc-file-pkm.html
fi

if [[ -n "$SAV_FIXTURE" && -f "$SAV_FIXTURE" ]]; then
    echo "==> save json: $SAV_FIXTURE"
    "$SWIFT_BIN" "$DYLIB" save "$SAV_FIXTURE"
    echo "==> save html: $SAV_FIXTURE -> /tmp/pkmds-poc-save.html"
    "$SWIFT_BIN" "$DYLIB" save-html "$SAV_FIXTURE" > /tmp/pkmds-poc-save.html
    echo "==> file-html (save): $SAV_FIXTURE -> /tmp/pkmds-poc-file-save.html"
    "$SWIFT_BIN" "$DYLIB" file-html "$SAV_FIXTURE" > /tmp/pkmds-poc-file-save.html
fi
