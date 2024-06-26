namespace Pkmds.Web;

public class ServerAppSettings
{
    public CorsPolicies CorsPolicies { get; set; } = default!;
}

// ReSharper disable once ClassNeverInstantiated.Global
public class CorsPolicies
{
    public string[]? AllowedOrigins { get; set; }
}
