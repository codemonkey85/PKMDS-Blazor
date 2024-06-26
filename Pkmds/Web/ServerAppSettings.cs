namespace Pkmds.Web;

public class ServerAppSettings
{
    public CorsPolicies CorsPolicies { get; set; } = default!;
}

public class CorsPolicies
{
    public string[]? AllowedOrigins { get; set; }
}
