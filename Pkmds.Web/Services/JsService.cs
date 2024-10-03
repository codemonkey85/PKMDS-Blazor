namespace Pkmds.Web.Services;

public class JsService(IJSRuntime js)
{
    private IJSInProcessRuntime SyncJs => js as IJSInProcessRuntime ??
                                      throw new NotSupportedException(
                                          "Requested an in process javascript interop, but none was found");

    public void EncryptAes(ReadOnlySpan<byte> origin, Span<byte> destination, ReadOnlySpan<byte> key, CipherMode mode)
    {
        var originHex = BitConverter.ToString(origin.ToArray()).Replace("-", "");
        var keyHex = BitConverter.ToString(key.ToArray()).Replace("-", "");

        var encryptedHex = SyncJs.Invoke<string>("encryptAes", keyHex, originHex, mode.ToString().ToLowerInvariant());

        var encryptedBytes = ConvertHexStringToByteArray(encryptedHex);
        encryptedBytes.CopyTo(destination);
    }

    public void DecryptAes(ReadOnlySpan<byte> origin, Span<byte> destination, ReadOnlySpan<byte> key, CipherMode mode)
    {
        var originHex = BitConverter.ToString(origin.ToArray()).Replace("-", "");
        var keyHex = BitConverter.ToString(key.ToArray()).Replace("-", "");

        var decryptedHex = SyncJs.Invoke<string>("decryptAes", keyHex, originHex, mode.ToString().ToLowerInvariant());

        var decryptedBytes = ConvertHexStringToByteArray(decryptedHex);
        decryptedBytes.CopyTo(destination);
    }

    private static byte[] ConvertHexStringToByteArray(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    public byte[] Md5Hash(byte[] toArray)
    {
        var toBeHashedHexString = BitConverter.ToString(toArray.ToArray()).Replace("-", "");
        var hashedHexString = SyncJs.Invoke<string>("md5Hash", toBeHashedHexString);
        return ConvertHexStringToByteArray(hashedHexString);
    }
}
