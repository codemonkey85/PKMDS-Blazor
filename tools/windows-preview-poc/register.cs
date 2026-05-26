#:project ../preview-shared/Pkmds.Preview.csproj
#:property TargetFramework=net10.0-windows

// Registers / unregisters the PKMDS Windows Explorer preview handler.
//
// The COM object that Explorer loads is the native PkmdsPreviewShim.dll; this script just
// writes the registry keys Explorer needs, reading the shared Pkmds.Preview.PreviewFileTypes
// extension list so the set stays single-sourced with the macOS/iOS PoCs. It's a `dotnet run`
// file-based app because PowerShell 7 runs on an older runtime than this project targets.
//
//   dotnet run register.cs -- --register "<abs path to PkmdsPreviewShim.dll>"   (elevated)
//   dotnet run register.cs -- --unregister                                       (elevated)
//   dotnet run register.cs -- --list                                             (no admin)

using Microsoft.Win32;
using Pkmds.Preview;

const string HandlerClsid = "{e528b90b-bba4-4870-92fe-d8ee781d86c5}";
const string FriendlyName = "PKMDS Preview Handler";
// 64-bit Preview Handler Surrogate Host (System32\prevhost.exe) — the shim is built x64.
const string SurrogateAppId = "{6d2b5079-2f0b-48dd-ab7f-97cec514d30b}";
// IPreviewHandler shell-extension IID — fixed by Windows.
const string PreviewHandlerIid = "{8895b1c6-b41f-4c1c-a562-0d564250836f}";
const string PreviewHandlersKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PreviewHandlers";

var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "--help";

switch (mode)
{
    case "--register":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("--register requires the absolute path to PkmdsPreviewShim.dll");
            Environment.Exit(2);
        }
        Register(Path.GetFullPath(args[1]));
        Console.WriteLine($"Registered PKMDS preview handler for {PreviewFileTypes.Extensions.Length} extensions.");
        Console.WriteLine("Restart Explorer (or sign out/in) for the handler to load.");
        break;

    case "--unregister":
        Unregister();
        Console.WriteLine("Unregistered PKMDS preview handler.");
        break;

    case "--list":
        Console.WriteLine($"{PreviewFileTypes.Extensions.Length} extensions:");
        Console.WriteLine(string.Join(' ', PreviewFileTypes.Extensions));
        break;

    default:
        Console.WriteLine("Usage: dotnet run register.cs -- [--register <PkmdsPreviewShim.dll> | --unregister | --list]");
        Console.WriteLine("  --register / --unregister require an elevated (Administrator) terminal.");
        break;
}

// Machine-wide registration (HKLM via HKCR). Mirrors how the built-in preview handlers are
// registered: a plain in-proc COM server (InprocServer32 -> the shim DLL, Apartment threading),
// the surrogate AppID, an entry in the approved PreviewHandlers list, and a per-extension ShellEx.
void Register(string shimDllPath)
{
    if (!File.Exists(shimDllPath))
        throw new FileNotFoundException("Shim DLL not found — build it first (build-shim.ps1).", shimDllPath);

    using (var clsid = Registry.ClassesRoot.CreateSubKey($@"CLSID\{HandlerClsid}"))
    {
        clsid.SetValue(null, FriendlyName);
        clsid.SetValue("AppID", SurrogateAppId);
        using var inproc = clsid.CreateSubKey("InprocServer32");
        inproc.SetValue(null, shimDllPath);
        inproc.SetValue("ThreadingModel", "Apartment");
    }

    using (var list = Registry.LocalMachine.CreateSubKey(PreviewHandlersKey))
        list.SetValue(HandlerClsid, FriendlyName);

    foreach (var ext in PreviewFileTypes.Extensions)
    {
        using var shellEx = Registry.ClassesRoot.CreateSubKey($@"{ext}\ShellEx\{PreviewHandlerIid}");
        shellEx.SetValue(null, HandlerClsid);
    }
}

void Unregister()
{
    foreach (var ext in PreviewFileTypes.Extensions)
        Registry.ClassesRoot.DeleteSubKeyTree($@"{ext}\ShellEx\{PreviewHandlerIid}", throwOnMissingSubKey: false);

    using (var list = Registry.LocalMachine.OpenSubKey(PreviewHandlersKey, writable: true))
        list?.DeleteValue(HandlerClsid, throwOnMissingValue: false);

    Registry.ClassesRoot.DeleteSubKeyTree($@"CLSID\{HandlerClsid}", throwOnMissingSubKey: false);
}
