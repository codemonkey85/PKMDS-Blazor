# Windows Shell Preview Handler POC

Proof-of-concept stub for a Windows Explorer Preview Pane handler that previews PKHeX-compatible files (`.pk1`–`.pk9`, `.pa8`, `.pb7`, `.pb8`, `.sav`) using the shared `HtmlRenderer` from [`tools/preview-shared/`](../preview-shared/).

The macOS and iOS equivalents are in [`tools/macos-quicklook-poc/`](../macos-quicklook-poc/) and [`tools/ios-quicklook-poc/`](../ios-quicklook-poc/).

## What's in here

```
tools/windows-preview-poc/
├── PkmdsPreview/
│   ├── PreviewHandler.cs    # COM shell extension stub (IPreviewHandler + WebView2 TODOs)
│   └── PkmdsPreview.csproj  # net10.0-windows + EnableComHosting + UseWindowsForms
└── README.md
```

`HtmlRenderer` (the actual rendering logic) lives in `tools/preview-shared/Pkmds.Preview.csproj` and is shared across all three platform PoCs.

## Prerequisites

- **Windows 10/11** with .NET SDK 10 installed.
- **Visual Studio 2022** (or VS Build Tools 2022) with the `.NET desktop development` workload.
- **WebView2 Runtime** — ships with Windows 11 and recent Windows 10 updates; otherwise install from [aka.ms/webview2](https://developer.microsoft.com/microsoft-edge/webview2/).

## Before building: one-time setup

### 1. Add `Microsoft.Web.WebView2` to central package management

In `Directory.Packages.props` (repo root), add:

```xml
<PackageVersion Include="Microsoft.Web.WebView2" Version="1.0.3296.44" />
```

Check [nuget.org/packages/Microsoft.Web.WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2) for the latest stable version before pinning.

### 2. Uncomment the PackageReference in the csproj

In `PkmdsPreview/PkmdsPreview.csproj`, uncomment:

```xml
<PackageReference Include="Microsoft.Web.WebView2"/>
```

### 3. Generate a real handler GUID

The stub ships a placeholder GUID. Replace it with a freshly generated one in **both** places it appears in `PreviewHandler.cs` (the `[Guid(...)]` attribute and `HandlerGuid`):

```powershell
[guid]::NewGuid()   # PowerShell
# or Tools > Create GUID in Visual Studio
```

## Build

```powershell
dotnet build tools/windows-preview-poc/PkmdsPreview/PkmdsPreview.csproj -c Debug
```

The output directory will contain:

```
PkmdsPreview.dll           # managed assembly
PkmdsPreview.comhost.dll   # native COM host (the file regsvr32 registers)
Pkmds.Preview.dll          # shared HTML renderer
Pkmds.Core.dll             # PKHeX utilities
PKHeX.Core.dll             # PKHeX library
```

## Wiring up WebView2 (the main TODO)

`PreviewHandler.cs` has four marked TODO sites to complete:

1. **`SetWindow`** — create the `WebView2` WinForms control as a child of the `hwnd` passed in, and call `EnsureCoreWebView2Async()`. WebView2 init is async; set a `_webViewReady` flag in the `CoreWebView2InitializationCompleted` handler.

2. **`SetRect` / `SetWindow`** — call `_webView.SetBounds(prc.ToRectangle())` to resize the control when the preview pane resizes.

3. **`DoPreview`** — trigger the render once `_webViewReady` is true. If `SetWindow` fires before `DoPreview`, queue a render; if `DoPreview` fires first, let `CoreWebView2InitializationCompleted` trigger it.

4. **`Unload`** — dispose the `WebView2` control.

The actual HTML comes from `HtmlRenderer.RenderPkm` / `HtmlRenderer.RenderSave` (already wired in `RenderIfReady()`); pass it to `_webView.CoreWebView2.NavigateToString(html)`.

### WebView2 async pattern sketch

```csharp
public void SetWindow(nint hwnd, ref RECT prc)
{
    _parentHwnd = hwnd;
    _previewRect = prc;

    _webView = new WebView2();
    _webView.SetBounds(prc.ToRectangle());
    _webView.CoreWebView2InitializationCompleted += (_, _) =>
    {
        _webViewReady = true;
        RenderIfReady();
    };
    NativeMethods.SetParent(_webView.Handle, hwnd);
    _ = _webView.EnsureCoreWebView2Async();
}

public void DoPreview()
{
    if (_webViewReady)
        RenderIfReady();
    // else: CoreWebView2InitializationCompleted will call RenderIfReady() when ready
}
```

## Registration

The build emits `PkmdsPreview.comhost.dll` — this is the in-process COM server that Explorer's `prevhost.exe` loads.

### Register (admin PowerShell, from the build output directory)

```powershell
# 1. Register the COM class (writes HKCR\CLSID\{guid}\...)
regsvr32 PkmdsPreview.comhost.dll

# 2. Register the shell extension for each file extension
#    (writes HKCR\.pk5\ShellEx\{8895b1c6-...} = {your-guid})
#    Call via dotnet-script or a small host EXE — or run manually:
dotnet-script -e "[Pkmds.Preview.Windows.PkmdsPreviewHandler]::RegisterShellExtension()"
```

### Unregister

```powershell
dotnet-script -e "[Pkmds.Preview.Windows.PkmdsPreviewHandler]::UnregisterShellExtension()"
regsvr32 /u PkmdsPreview.comhost.dll
```

### What the registry keys look like

```
HKCR
└── .pk5
│   └── ShellEx
│       └── {8895b1c6-b41f-4c1c-a562-0d564250836f}   ← IPreviewHandler IID (fixed)
│           └── (Default) = {your-handler-GUID}
└── CLSID
    └── {your-handler-GUID}                           ← written by regsvr32
        ├── (Default) = "PKMDS Preview Handler"
        └── InprocServer32
            └── (Default) = C:\...\PkmdsPreview.comhost.dll
```

Repeat the `.pk5` entry for each extension (`.pk1`–`.pk9`, `.pa8`, `.pb7`, `.pb8`, `.sav`). `RegisterShellExtension()` handles all of them in one call.

## Architecture decisions

### Why an in-process COM server, not a subprocess

Explorer's Preview Pane calls `IPreviewHandler::DoPreview()` synchronously on a deadline. An out-of-process helper (subprocess + IPC) adds latency and complexity. `EnableComHosting=true` gives us a native COM host (comhost.dll) that loads into `prevhost.exe` directly — same pattern as macOS NativeAOT loading into `quicklookd`.

### Why WebView2 instead of WinForms `WebBrowser`

The legacy `WebBrowser` control is IE-based (Trident engine) and can't render modern CSS (`color-scheme`, `color-mix`, CSS Grid, `@media`). WebView2 is Edge/Chromium and renders `HtmlRenderer`'s output identically to macOS/iOS `WKWebView`.

### Why `IInitializeWithFile` not `IInitializeWithStream`

`IInitializeWithFile` is simpler and sufficient for local files. `IInitializeWithStream` would be needed for files accessed through a virtual filesystem (e.g. inside a ZIP opened in Explorer). For the PoC, file-based init covers all real-world cases.

### Why shared `HtmlRenderer` not the Blazor components

See the [macOS PoC README](../macos-quicklook-poc/README.md#why-we-dont-reuse-the-razor-components) — same reasoning applies here.

## COM interfaces vs. CsWin32

`PreviewHandler.cs` defines `IPreviewHandler`, `IInitializeWithFile`, `RECT`, and `MSG` manually. For production code, consider replacing them with CsWin32-generated types:

1. Add to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="Microsoft.Windows.CsWin32" Version="0.3.x" />
   <PackageVersion Include="Microsoft.Windows.SDK.Win32Metadata" Version="..." />
   ```
2. Add to `PkmdsPreview.csproj`:
   ```xml
   <PackageReference Include="Microsoft.Windows.CsWin32" PrivateAssets="all"/>
   ```
3. Create `NativeMethods.txt` with:
   ```
   IPreviewHandler
   IInitializeWithFile
   SetParent
   GetFocus
   ```
4. Delete the manual `[ComImport]` interface declarations in `PreviewHandler.cs`.

CsWin32 generates the correct vtable layout, marshaling attributes, and struct sizes from the official Win32 metadata — less error-prone than hand-authoring interop.

## Open work

- [ ] Implement `SetWindow` / `SetRect` / `DoPreview` / `Unload` with a real `WebView2` control (see TODO sketch above).
- [ ] Replace the placeholder GUID with a real one.
- [ ] Uncomment `PackageReference` for `Microsoft.Web.WebView2` (after adding to `Directory.Packages.props`).
- [ ] Write a small install script or MSI that runs `regsvr32` + `RegisterShellExtension()` — users shouldn't need to do this manually.
- [ ] Test against the same fixtures used by the macOS/iOS PoCs.
- [ ] Verify the 120 MB memory ceiling (prevhost.exe's default) against a full-box save file.
- [ ] Consider `IInitializeWithStream` for future virtual-filesystem support.
