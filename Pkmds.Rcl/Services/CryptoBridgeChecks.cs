using System.Security.Cryptography;

namespace Pkmds.Rcl.Services;

/// <summary>Known answers through PKHeX's configured providers, not direct JS-only calls.</summary>
public static class CryptoBridgeChecks
{
    public static Dictionary<string, string> Run()
    {
        // NIST SP 800-38A, F.1.1 and F.2.1. Check both directions independently;
        // a matching encrypt/decrypt bug must not pass a round-trip-only check.
        const string key = "2b7e151628aed2a6abf7158809cf4f3c";
        const string plaintext = "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e51" +
                                 "30c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710";
        const string ecb = "3ad77bb40d7a3660a89ecaf32466ef97f5d3d58503b9699de785895a96fdbaaf" +
                           "43b1cd7f598ece23881b00e3ed0306887b0c785e27e8ad3f8223207104725dd4";
        const string cbc = "7649abac8119b246cee98e9b12e9197d5086cb9b507219ee95db113a917678b2" +
                           "73bed6b8e3c1743b7116e69e222295163ff1caa1681fac09120eca307586e1a7";
        var results = new Dictionary<string, string>();
        foreach (var mode in new[] { CipherMode.ECB, CipherMode.CBC })
        {
            var expected = Convert.FromHexString(mode == CipherMode.ECB ? ecb : cbc);
            var input = Convert.FromHexString(plaintext);
            var iv = mode == CipherMode.CBC ? Convert.FromHexString("000102030405060708090a0b0c0d0e0f") : null;
            using var aes = RuntimeCryptographyProvider.Aes.Create(Convert.FromHexString(key), mode, PaddingMode.None, iv);
            var output = new byte[input.Length];
            if (mode == CipherMode.ECB) aes.EncryptEcb(input, output);
            else aes.EncryptCbc(input, output);
            RequireEqual(output, expected, $"AES-{mode} encryption");
            results[$"aes-{mode}-encrypt"] = Convert.ToHexString(output);
            if (mode == CipherMode.ECB) aes.DecryptEcb(expected, output);
            else aes.DecryptCbc(expected, output);
            RequireEqual(output, input, $"AES-{mode} decryption");
            results[$"aes-{mode}-decrypt"] = Convert.ToHexString(output);
        }

        // RFC 1321 A.5, including the empty input and multiple MD5 blocks.
        foreach (var (text, expected) in new[]
                 {
                     ("", "d41d8cd98f00b204e9800998ecf8427e"),
                     ("abc", "900150983cd24fb0d6963f7d28e17f72"),
                     (string.Concat(Enumerable.Repeat("1234567890", 8)), "57edf4a22be3c955ac49da2e2107b67a"),
                 })
        {
            var output = new byte[16];
            RuntimeCryptographyProvider.Md5.HashData(Encoding.UTF8.GetBytes(text), output);
            RequireEqual(output, Convert.FromHexString(expected), "MD5");
            results[$"md5-{text.Length}-bytes"] = Convert.ToHexString(output);
        }
        return results;
    }

    private static void RequireEqual(byte[] actual, byte[] expected, string operation)
    {
        if (!actual.AsSpan().SequenceEqual(expected))
        {
            throw new CryptographicException($"{operation} did not match the known answer.");
        }
    }
}
