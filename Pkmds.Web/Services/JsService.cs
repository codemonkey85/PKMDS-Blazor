namespace Pkmds.Web.Services;

/// <summary>
/// Service for JavaScript interop, providing cryptographic operations via crypto-js library.
/// This service bridges .NET code with JavaScript implementations of AES and MD5,
/// which are necessary because these cryptographic APIs are not fully supported in Blazor WebAssembly.
/// </summary>
public class JsService(IJSRuntime js)
{
    /// <summary>
    /// Gets the synchronous JavaScript runtime, throwing if not available.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when IJSInProcessRuntime is not available.</exception>
    private IJSInProcessRuntime SyncJs =>
        js as IJSInProcessRuntime
        ?? throw new NotSupportedException("Requested an in process javascript interop, but none was found");

    /// <summary>
    /// Encrypts data using AES with the specified key and cipher mode.
    /// </summary>
    /// <param name="origin">The plaintext data to encrypt.</param>
    /// <param name="destination">The span to write the encrypted data to.</param>
    /// <param name="key">The encryption key.</param>
    /// <param name="mode">The cipher mode (ECB or CBC).</param>
    public void EncryptAes(ReadOnlySpan<byte> origin, Span<byte> destination, ReadOnlySpan<byte> key, CipherMode mode)
    {
        var originHex = BitConverter.ToString(origin.ToArray()).Replace("-", "");
        var keyHex = BitConverter.ToString(key.ToArray()).Replace("-", "");

        var encryptedHex = SyncJs.Invoke<string>("encryptAes", keyHex, originHex, mode.ToString().ToLowerInvariant());

        var encryptedBytes = ConvertHexStringToByteArray(encryptedHex);
        encryptedBytes.CopyTo(destination);
    }

    /// <summary>
    /// Decrypts data using AES with the specified key and cipher mode.
    /// </summary>
    /// <param name="origin">The ciphertext data to decrypt.</param>
    /// <param name="destination">The span to write the decrypted data to.</param>
    /// <param name="key">The decryption key.</param>
    /// <param name="mode">The cipher mode (ECB or CBC).</param>
    public void DecryptAes(ReadOnlySpan<byte> origin, Span<byte> destination, ReadOnlySpan<byte> key, CipherMode mode)
    {
        var originHex = BitConverter.ToString(origin.ToArray()).Replace("-", "");
        var keyHex = BitConverter.ToString(key.ToArray()).Replace("-", "");

        var decryptedHex = SyncJs.Invoke<string>("decryptAes", keyHex, originHex, mode.ToString().ToLowerInvariant());

        var decryptedBytes = ConvertHexStringToByteArray(decryptedHex);
        decryptedBytes.CopyTo(destination);
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hexadecimal string (without separators).</param>
    /// <returns>The byte array representation.</returns>
    private static byte[] ConvertHexStringToByteArray(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// Computes the MD5 hash of the input data using JavaScript interop.
    /// </summary>
    /// <param name="toArray">The data to hash.</param>
    /// <returns>The MD5 hash as a byte array (16 bytes).</returns>
    public byte[] Md5Hash(byte[] toArray)
    {
        var toBeHashedHexString = BitConverter.ToString(toArray.ToArray()).Replace("-", "");
        var hashedHexString = SyncJs.Invoke<string>("md5Hash", toBeHashedHexString);
        return ConvertHexStringToByteArray(hashedHexString);
    }
}
