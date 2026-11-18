using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Pkmds.WorkerPrototype;

// dotnet.create() doesn't start Blazor's UI dispatcher. This narrow dispatcher
// calls the real production crypto functions via supported synchronous JS imports.
// The app's /bench?suite=crypto separately exercises actual Blazor DI/interop.
[SupportedOSPlatform("browser")]
internal sealed partial class PrototypeJsRuntime : IJSInProcessRuntime
{
    public TValue Invoke<TValue>(string identifier, params object?[]? args)
    {
        args ??= [];
        try
        {
            var result = identifier switch
            {
                "encryptAes" => Encrypt((string)args[0]!, (string)args[1]!, (string)args[2]!, (string?)args[3]),
                "decryptAes" => Decrypt((string)args[0]!, (string)args[1]!, (string)args[2]!, (string?)args[3]),
                "md5Hash" => Md5((string)args[0]!),
                _ => throw new NotSupportedException("Only production crypto calls are allowed in the prototype."),
            };
            return (TValue)(object)result;
        }
        catch (System.Runtime.InteropServices.JavaScript.JSException exception)
        {
            throw new Microsoft.JSInterop.JSException(exception.Message);
        }
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(Invoke<TValue>(identifier, args));

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return InvokeAsync<TValue>(identifier, args);
    }

    [JSImport("globalThis.encryptAes")]
    private static partial string Encrypt(string key, string data, string mode, string? iv);

    [JSImport("globalThis.decryptAes")]
    private static partial string Decrypt(string key, string data, string mode, string? iv);

    [JSImport("globalThis.md5Hash")]
    private static partial string Md5(string data);
}
