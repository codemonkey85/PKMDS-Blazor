namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Lightweight file tracing for the worker. The worker runs windowless inside the preview
/// flow with no console, so failures are invisible otherwise. Writes to a LocalLow path
/// (writable even at Low integrity). Best-effort — never throws.
/// </summary>
internal static class Diag
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "AppData", "LocalLow", "PkmdsPreview");

    // Tracing is off unless a sentinel file exists, so normal previews write nothing. To debug
    // a session, create an empty file at <LocalLow>\PkmdsPreview\worker.trace; the worker then
    // appends to worker.log. LocalLow is writable even at Low integrity.
    private static readonly bool Enabled = File.Exists(Path.Combine(Dir, "worker.trace"));

    public static void Log(string message)
    {
        if (!Enabled)
            return;
        try
        {
            Directory.CreateDirectory(Dir);
            File.AppendAllText(
                Path.Combine(Dir, "worker.log"),
                $"{DateTime.Now:HH:mm:ss.fff} [pid {Environment.ProcessId}] {message}{Environment.NewLine}");
        }
        catch
        {
            // tracing must never break the preview
        }
    }
}
