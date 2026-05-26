# Windows Shell Preview Handler PoC

Previews PKHeX-compatible files in the Windows Explorer **Preview Pane**, using the shared
`HtmlRenderer` from [`tools/preview-shared/`](../preview-shared/) — the exact same rendering code
the [macOS](../macos-quicklook-poc/) and [iOS](../ios-quicklook-poc/) Quick Look PoCs use. All
parsing and HTML generation is shared; only the platform host differs.

**Status: working.** Pokémon entities, save files, and wonder cards render in the pane. Known
limitation: the preview is rendered once at the initial pane size and does **not** reflow when
you drag the pane wider/narrower (see [Known limitations](#known-limitations--future-work)).

![preview pane showing a rendered Pokémon](https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/home/448.png)

## Architecture: native shim + .NET worker

Unlike macOS/iOS (where Quick Look loads a NativeAOT **dylib** — statically-linked native code),
a Windows preview handler is loaded into `prevhost.exe`, which runs handlers in a **Low-integrity
sandbox**. A framework-dependent .NET **comhost** cannot start CoreCLR in that sandbox, so the
managed code never runs (see [Why not a pure-.NET handler](#why-not-a-pure-net-comhost-handler)).

The solution — mirroring how [PowerToys](https://github.com/microsoft/PowerToys/tree/main/src/modules/previewpane)
ships working .NET preview handlers — is a **two-part split**:

```
Explorer Preview Pane
        │  loads (COM IPreviewHandler)
        ▼
PkmdsPreviewShim.dll   ← native C++, runs fine at Low IL (no runtime to host)
        │  DoPreview(): ShellExecuteEx with  "<file>" <hwnd-hex> <left> <right> <top> <bottom>
        ▼
PkmdsPreviewWorker.exe ← self-contained .NET WinExe (normal process: CoreCLR + WebView2 work)
        │  reparents its WebView2 window into <hwnd>, sizes to the rect
        ▼
HtmlRenderer.RenderFile(bytes, ext)   ← shared with macOS/iOS
```

- **`PkmdsPreviewShim`** (C++ DLL) is the registered COM `IPreviewHandler`. On `DoPreview` it
  `ShellExecuteEx`'s the worker, passing the file path, parent `HWND`, and bounds on the command
  line; on `Unload` it `TerminateProcess`'s the worker. It's a thin shim — no rendering logic.
- **`PkmdsPreviewWorker`** (.NET `WinExe`, WinForms, **self-contained**) runs as an ordinary
  full-trust process. `Program.Main` parses the args, reparents its `WebView2` into the supplied
  `HWND` (`WS_CHILD` + `SetParent`), and renders the shared `HtmlRenderer` output via
  `NavigateToString`.

### Single source of truth for extensions

The authoritative extension list is [`Pkmds.Preview.PreviewFileTypes`](../preview-shared/PreviewFileTypes.cs)
(shared project), grouped into the three categories that map 1:1 to the macOS/iOS UTType
declarations (`pkm-file`, `save-file`, `wonder-card`). `register.cs` reads it directly; the
macOS/iOS `Info.plist` `UTTypeTagSpecification` arrays mirror it. Coverage:

- **Pokémon entity files**: `.pk1`–`.pk9`, `.pa8`, `.pa9`, `.pb7`, `.pb8`, `.sk2`, `.ck3`, `.xk3`, `.bk4`, `.rk4`
- **Save files**: `.sav`, `.dat`, `.gci`, `.dsv`, `.srm`, `.fla`
- **Wonder cards / mystery gifts**: `.pgt`, `.pcd`, `.pgf`, `.wc3`–`.wc9` and their `*full` / `.wb*` / `.wa*` variants

## What's in here

```
tools/windows-preview-poc/
├── PkmdsPreviewShim/          # native C++ COM IPreviewHandler (loads into prevhost)
│   ├── PkmdsPreviewShim.cpp
│   ├── PkmdsPreviewShim.def   # exports DllGetClassObject + DllCanUnloadNow
│   └── PkmdsPreviewShim.vcxproj
├── PkmdsPreviewWorker/        # self-contained .NET WinExe worker (renders)
│   ├── Program.cs             # arg parsing + mode dispatch (child / --window / --capture)
│   ├── PreviewForm.cs         # WebView2 host, reparents into the pane HWND
│   ├── Capture.cs             # headless render-to-PNG (dev verification)
│   └── Diag.cs                # opt-in file tracing
├── build-shim.ps1            # build the C++ shim via vcvars + cl
├── register.cs              # dotnet-run registrar (reads shared PreviewFileTypes)
├── install.ps1 / uninstall.ps1
└── README.md
```

## Prerequisites

- **Windows 10/11**, .NET SDK from `global.json` (net10.0).
- **MSVC / C++ toolchain** — Visual Studio (or Build Tools) with the **"Desktop development with
  C++"** workload, to build the shim. `build-shim.ps1` locates it via `vswhere`.
- **WebView2 Runtime** — ships with Windows 11 / recent Windows 10; else [aka.ms/webview2](https://developer.microsoft.com/microsoft-edge/webview2/).

## Build & install

Registration writes machine-wide keys (`HKLM`), so install from an **elevated** PowerShell:

```powershell
./tools/windows-preview-poc/install.ps1            # build shim + publish worker + register + restart Explorer
./tools/windows-preview-poc/uninstall.ps1          # remove all keys + restart Explorer
```

`install.ps1` (1) builds `PkmdsPreviewShim.dll`, (2) publishes the worker self-contained into
`dist\`, (3) co-locates the shim there, (4) registers `InprocServer32` → `dist\PkmdsPreviewShim.dll`,
(5) restarts Explorer. Then select a PKHeX file with the Preview Pane on (`Alt+P`).

Build pieces individually if needed:

```powershell
./tools/windows-preview-poc/build-shim.ps1                         # -> PkmdsPreviewShim\bin\PkmdsPreviewShim.dll
dotnet publish tools/windows-preview-poc/PkmdsPreviewWorker/PkmdsPreviewWorker.csproj -c Release -r win-x64 --self-contained -o tools/windows-preview-poc/dist
dotnet run tools/windows-preview-poc/register.cs -- --list         # print registered extensions (no admin)
```

The worker also has standalone modes for development (no shell needed):

```powershell
PkmdsPreviewWorker.exe --window "C:\path\file.pk9"            # show in a normal window
PkmdsPreviewWorker.exe --capture "out.png" "C:\path\file.pk9" # headless render to PNG
```

### Registration keys

`register.cs` writes (matching the built-in TXT/PDF handlers):

```
HKLM\SOFTWARE\Classes\CLSID\{e528b90b-…}
├── (Default)        = "PKMDS Preview Handler"
├── AppID            = {6d2b5079-…}            ← 64-bit Preview Handler Surrogate Host
└── InprocServer32
    ├── (Default)    = …\dist\PkmdsPreviewShim.dll
    └── ThreadingModel = "Apartment"
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PreviewHandlers\{e528b90b-…} = "PKMDS Preview Handler"
HKLM\SOFTWARE\Classes\.pk5\ShellEx\{8895b1c6-…} = {e528b90b-…}     ← per extension
```

## How it works (details)

Explorer calls the shim roughly: `Initialize`(file) → `SetWindow`(hwnd, rect) → `SetRect` →
`DoPreview`. The shim launches the worker on the first real (non-empty) rect.

Two environment details the worker must respect:

- **LocalLow paths.** The worker can inherit Low integrity from `prevhost`, where `%LOCALAPPDATA%`
  and `%TEMP%` aren't writable. WebView2's user-data folder (and the optional trace log) live under
  `%USERPROFILE%\AppData\LocalLow\PkmdsPreview` instead.
- **PerMonitorV2 DPI.** The worker sets `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`
  to match `prevhost`. Without it the reparented child window is bitmap-scaled by the display
  factor (e.g. 1.5× at 150%), clipping content off the right edge.

Also note: `SetParent` returns the *previous* parent (NULL for a top-level form even on success),
so the worker checks `GetLastError`, not the return value, to detect failure.

### Why not a pure-.NET (comhost) handler?

The first attempt registered a .NET assembly via `EnableComHosting` (a `comhost.dll`) directly as
the `IPreviewHandler`. It built and registered correctly but **never rendered** — diagnosis showed
the managed code never ran. A framework-dependent **CoreCLR comhost cannot be activated inside the
Low-integrity `prevhost` sandbox**; `DisableLowILProcessIsolation` was ignored, and self-contained
deployment is unsupported for COM hosting (`NETSDK1128`). That's why the registered object must be
native (the shim) and the .NET code must run as a separate process (the worker).

### Diagnostics

The worker traces only if a sentinel exists: create an empty
`%USERPROFILE%\AppData\LocalLow\PkmdsPreview\worker.trace`, reproduce, then read `worker.log` in the
same folder (start args, reparent result + DPI, WebView2 init, render size). Delete the sentinel to
turn it off.

## Known limitations / future work

- [ ] **Resize/reflow.** The preview renders once at the initial pane size; dragging the pane
      doesn't re-layout. PowerToys signals the worker via a named event on `SetRect` — port that.
- [ ] Replace the manual C++ COM with WIL/wrl helpers, or generate the shim via NativeAOT C#
      (would keep everything in .NET, but still needs the MSVC linker).
- [ ] Package as an MSI/MSIX so end users don't run scripts elevated.
- [ ] Verify the `prevhost` memory ceiling against a full-box save file.
- [ ] `IInitializeWithStream` for files inside virtual filesystems (ZIP, etc.).
