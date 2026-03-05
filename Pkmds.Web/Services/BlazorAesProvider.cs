namespace Pkmds.Web.Services;

/// <summary>
/// Blazor WebAssembly implementation of AES cryptography provider.
/// Uses JavaScript interop (via crypto-js) to perform AES encryption/decryption
/// because System.Security.Cryptography is not fully supported in WASM.
/// This is required for PKHeX.Core's encryption/decryption of save files and Pokémon data.
/// </summary>
public class BlazorAesProvider(JsService jsService) : IAesCryptographyProvider
{
    /// <summary>
    /// Creates an AES cryptography instance with the specified parameters.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="mode">The cipher mode (ECB or CBC).</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="iv">The initialization vector (optional, used for CBC mode).</param>
    /// <returns>An IAes instance for performing encryption/decryption.</returns>
    public IAesCryptographyProvider.IAes Create(byte[] key, CipherMode mode, PaddingMode padding, byte[]? iv = null) =>
        new CryptoJsAes(jsService, key, mode, padding, iv);

    /// <summary>
    /// Internal class that implements AES encryption/decryption using JavaScript interop.
    /// </summary>
#pragma warning disable CS9113 // Parameter is unread.
    private class CryptoJsAes(JsService jsService, byte[] key, CipherMode mode, PaddingMode padding, byte[]? iv = null)
#pragma warning restore CS9113 // Parameter is unread.
        : IAesCryptographyProvider.IAes
    {
        /// <summary>
        /// Encrypts data using AES in ECB mode.
        /// </summary>
        public void EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination) =>
            jsService.EncryptAes(plaintext, destination, key, CipherMode.ECB);

        /// <summary>
        /// Decrypts data using AES in ECB mode.
        /// </summary>
        public void DecryptEcb(ReadOnlySpan<byte> ciphertext, Span<byte> destination) =>
            jsService.DecryptAes(ciphertext, destination, key, CipherMode.ECB);

        /// <summary>
        /// Encrypts data using AES in CBC mode.
        /// </summary>
        public void EncryptCbc(ReadOnlySpan<byte> plaintext, Span<byte> destination) =>
            jsService.EncryptAes(plaintext, destination, key, CipherMode.CBC);

        /// <summary>
        /// Decrypts data using AES in CBC mode.
        /// </summary>
        public void DecryptCbc(ReadOnlySpan<byte> ciphertext, Span<byte> destination) =>
            jsService.DecryptAes(ciphertext, destination, key, CipherMode.CBC);

        /// <summary>
        /// Disposes the AES instance (no-op for JavaScript-based implementation).
        /// </summary>
        public void Dispose() { }
    }
}
