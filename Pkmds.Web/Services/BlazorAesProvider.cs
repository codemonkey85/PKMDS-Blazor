namespace Pkmds.Web.Services;

public class BlazorAesProvider(JsService jsService) : IAesCryptographyProvider
{
    public IAesCryptographyProvider.IAes Create(byte[] key, CipherMode mode, PaddingMode padding, byte[]? iv = null) =>
        new CryptoJsAes(jsService, key, mode, padding, iv);

#pragma warning disable CS9113 // Parameter is unread.
    private class CryptoJsAes(JsService jsService, byte[] key, CipherMode mode, PaddingMode padding, byte[]? iv = null)
#pragma warning restore CS9113 // Parameter is unread.
        : IAesCryptographyProvider.IAes
    {
        public void EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination) =>
            jsService.EncryptAes(plaintext, destination, key, CipherMode.ECB);

        public void DecryptEcb(ReadOnlySpan<byte> ciphertext, Span<byte> destination) =>
            jsService.DecryptAes(ciphertext, destination, key, CipherMode.ECB);

        public void EncryptCbc(ReadOnlySpan<byte> plaintext, Span<byte> destination) =>
            jsService.EncryptAes(plaintext, destination, key, CipherMode.CBC);

        public void DecryptCbc(ReadOnlySpan<byte> ciphertext, Span<byte> destination) =>
            jsService.DecryptAes(ciphertext, destination, key, CipherMode.CBC);

        public void Dispose() { }
    }
}
