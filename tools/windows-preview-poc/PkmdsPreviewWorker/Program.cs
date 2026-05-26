using System.Globalization;

namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Entry point for the preview worker. Three modes:
///
///   PkmdsPreviewWorker.exe "&lt;file&gt;" &lt;hwnd-hex&gt; &lt;left&gt; &lt;right&gt; &lt;top&gt; &lt;bottom&gt; &lt;resize-event&gt;
///       Child mode — how the C++ shim invokes us. Reparents into &lt;hwnd&gt;, renders, and
///       watches the named &lt;resize-event&gt; to re-fit when the pane is resized.
///
///   PkmdsPreviewWorker.exe --window "&lt;file&gt;"
///       Standalone top-level window. Handy for eyeballing a render without the shell.
///
///   PkmdsPreviewWorker.exe --capture "&lt;out.png&gt;" "&lt;file&gt;"
///       Headless: render off-screen, save a PNG, exit. Used to verify rendering in CI/dev.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Diag.Log($"worker start: {args.Length} args = [{string.Join(" | ", args)}]");
        Sprites.UseBundled();
        try
        {
            ApplicationConfiguration.Initialize();
        }
        catch (Exception ex)
        {
            Diag.Log($"ApplicationConfiguration.Initialize failed: {ex}");
        }

        switch (args)
        {
            // Child mode: "<file>" <hwnd-hex> <left> <right> <top> <bottom> <resize-event-name>
            case [var file, var hwndHex, var l, var r, var t, var b, var resizeEvent]:
                {
                    try
                    {
                        var hwnd = nint.Parse(hwndHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        var bounds = Rectangle.FromLTRB(
                            int.Parse(l, CultureInfo.InvariantCulture),
                            int.Parse(t, CultureInfo.InvariantCulture),
                            int.Parse(r, CultureInfo.InvariantCulture),
                            int.Parse(b, CultureInfo.InvariantCulture));
                        Diag.Log($"child mode: hwnd=0x{hwnd:X} rect(LTRB)={l},{t},{r},{b} => bounds {bounds.Width}x{bounds.Height} file={file}");

                        var form = new PreviewForm();
                        if (!form.AttachToHost(hwnd, bounds))
                        {
                            Diag.Log("AttachToHost returned false; exiting");
                            return;
                        }
                        form.Render(file);
                        form.WatchForResize(resizeEvent);
                        Application.Run(form);
                    }
                    catch (Exception ex)
                    {
                        Diag.Log($"child mode FAILED: {ex}");
                    }
                    break;
                }

            // Standalone window (manual visual check).
            case ["--window", var file]:
                {
                    var form = new PreviewForm();
                    form.ShowAsTopLevel(file);
                    Application.Run(form);
                    break;
                }

            // Headless render → PNG (automated verification).
            case ["--capture", var outPng, var file]:
                Capture.RenderToPng(file, outPng);
                break;

            default:
                MessageBox.Show(
                    "Usage:\n" +
                    "  PkmdsPreviewWorker \"<file>\" <hwnd-hex> <left> <right> <top> <bottom>\n" +
                    "  PkmdsPreviewWorker --window \"<file>\"\n" +
                    "  PkmdsPreviewWorker --capture \"<out.png>\" \"<file>\"",
                    "PkmdsPreviewWorker");
                break;
        }
    }
}
