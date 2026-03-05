namespace Pkmds.Web.Services;

/// <summary>
/// Blazor WebAssembly implementation of MD5 hashing provider.
/// Uses JavaScript interop (via crypto-js) to perform MD5 hashing
/// because System.Security.Cryptography is not fully supported in WASM.
/// This is required for PKHeX.Core's checksum validation of save files.
/// </summary>
public class BlazorMd5Provider(JsService jsService) : IMd5Provider
{
    /// <summary>
    /// Computes the MD5 hash of the input data.
    /// </summary>
    /// <param name="source">The data to hash.</param>
    /// <param name="destination">The span to write the hash to (must be at least 16 bytes).</param>
    public void HashData(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        var hash = jsService.Md5Hash(source.ToArray());
        hash.CopyTo(destination);
    }
}
