namespace Pkmds.Rcl.Services;

/// <inheritdoc />
public sealed class HostService : IHostService
{
    public HostService(NavigationManager navigationManager)
    {
        var uri = new Uri(navigationManager.Uri);
        HostName = ParseHostFromQuery(uri.Query);
        if (string.IsNullOrWhiteSpace(HostName))
        {
            return;
        }

        IsEmbedded = true;
        HostKind = HostKind.Generic;
    }

    public bool IsEmbedded { get; }

    public HostKind HostKind { get; }

    public string? HostName { get; }

    private static string? ParseHostFromQuery(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return null;
        }

        var query = queryString.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx < 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..eqIdx]);
            if (!string.Equals(key, "host", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = Uri.UnescapeDataString(pair[(eqIdx + 1)..]);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
