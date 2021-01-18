using System;
namespace PKMDSData
{
    public static class PokePrng
    {
        //internal static uint LCRNG(uint seed) => (seed * 0x41C64E6D + 0x00006073) & 0xFFFFFFFF;

        //internal static uint LCRNG(ref uint seed) => seed = (seed * 0x41C64E6D + 0x00006073) & 0xFFFFFFFF;

        public static void ShuffleArray(ref byte[] pkx)
        {
            uint pv = BitConverter.ToUInt32(pkx, 0);
            uint sv = ((pv & 0x3E000) >> 0xD) % 24;
            byte[] ekx = new byte[232];
            Array.Copy(pkx, ekx, 8);
            byte[] aloc = { 0, 0, 0, 0, 0, 0, 1, 1, 2, 3, 2, 3, 1, 1, 2, 3, 2, 3, 1, 1, 2, 3, 2, 3 };
            byte[] bloc = { 1, 1, 2, 3, 2, 3, 0, 0, 0, 0, 0, 0, 2, 3, 1, 1, 3, 2, 2, 3, 1, 1, 3, 2 };
            byte[] cloc = { 2, 3, 1, 1, 3, 2, 2, 3, 1, 1, 3, 2, 0, 0, 0, 0, 0, 0, 3, 2, 3, 2, 1, 1 };
            byte[] dloc = { 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 0, 0, 0, 0, 0, 0 };
            byte[] shlog = { aloc[sv], bloc[sv], cloc[sv], dloc[sv] };
            for (int b = 0; b < 4; b++)
            {
                Array.Copy(pkx, 8 + 56 * shlog[b], ekx, 8 + 56 * b, 56);
            }

            Array.Copy(ekx, pkx, pkx.Length);
        }

        public static Span<byte> ShuffleArray(Span<byte> pkxSpan)
        {
            uint pv = BitConverter.ToUInt32(pkxSpan.Slice(0, 4));

            uint sv = ((pv & 0x3E000) >> 0xD) % 24;
            byte[] ekx = new byte[232];
            Array.Copy(pkxSpan.ToArray(), ekx, 8);
            byte[] aloc = { 0, 0, 0, 0, 0, 0, 1, 1, 2, 3, 2, 3, 1, 1, 2, 3, 2, 3, 1, 1, 2, 3, 2, 3 };
            byte[] bloc = { 1, 1, 2, 3, 2, 3, 0, 0, 0, 0, 0, 0, 2, 3, 1, 1, 3, 2, 2, 3, 1, 1, 3, 2 };
            byte[] cloc = { 2, 3, 1, 1, 3, 2, 2, 3, 1, 1, 3, 2, 0, 0, 0, 0, 0, 0, 3, 2, 3, 2, 1, 1 };
            byte[] dloc = { 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 0, 0, 0, 0, 0, 0 };
            byte[] shlog = { aloc[sv], bloc[sv], cloc[sv], dloc[sv] };
            for (int b = 0; b < 4; b++)
            {
                Array.Copy(pkxSpan.ToArray(), 8 + 56 * shlog[b], ekx, 8 + 56 * b, 56);
            }

            return new Span<byte>(ekx);
        }

        //internal static void CryptArray(ref byte[] data)
        //{
        //    byte[] dataout = (byte[])data.Clone();
        //    uint pv = BitConverter.ToUInt32(dataout, 0);
        //    uint seed = pv;
        //    for (int i = 8; i < 232; i += 2)
        //    {
        //        Array.Copy(BitConverter.GetBytes((ushort)(BitConverter.ToUInt16(dataout, i) ^ (LCRNG(ref seed) >> 16))), 0, dataout, i, 2);
        //    }

        //    Array.Copy(dataout, data, data.Length);
        //}

        //internal static byte[] DecryptArray(byte[] ekx)
        //{
        //    byte[] pkx = (byte[])ekx.Clone();
        //    CryptArray(ref pkx);
        //    ShuffleArray(ref pkx);
        //    return pkx;
        //}

        //internal static byte[] EncryptArray(byte[] pkx)
        //{
        //    byte[] ekx = (byte[])pkx.Clone();
        //    for (int i = 0; i < 11; i++)
        //    {
        //        ShuffleArray(ref ekx);
        //    }

        //    CryptArray(ref ekx);
        //    return ekx;
        //}

        //internal static ushort GetCHK(byte[] data)
        //{
        //    ushort chk = 0;
        //    for (int i = 8; i < 232; i += 2)
        //    {
        //        chk += BitConverter.ToUInt16(data, i);
        //    }

        //    return chk;
        //}

        //internal static bool Verifychk(byte[] input)
        //{
        //    ushort checksum = 0;
        //    if (input.Length == 100 || input.Length == 80)  // Gen 3 Files
        //    {
        //        for (int i = 32; i < 80; i += 2)
        //        {
        //            checksum += BitConverter.ToUInt16(input, i);
        //        }

        //        return checksum == BitConverter.ToUInt16(input, 28);
        //    }
        //    switch (input.Length)
        //    {
        //        case 236:
        //        case 220:
        //        case 136:
        //            Array.Resize(ref input, 136);
        //            break;

        //        case 232:
        //        case 260:
        //            Array.Resize(ref input, 232);
        //            break;

        //        default:
        //            throw new Exception("Wrong sized input array to verifychecksum");
        //    }

        //    ushort chk = 0;
        //    for (int i = 8; i < input.Length; i += 2)
        //    {
        //        chk += BitConverter.ToUInt16(input, i);
        //    }

        //    return chk == BitConverter.ToUInt16(input, 0x6);
        //}

        //internal static uint GetPSV(uint PID) => Convert.ToUInt16(((PID >> 16) ^ (PID & 0xFFFF)) >> 4);

        //internal static uint GetTSV(uint TID, uint SID) => (TID ^ SID) >> 4;

        //internal static void EncryptPokemon(Pokemon pokemon)
        //{
        //    Array.Copy(encryptArray(pokemon.data), 0, pokemon.data, 0, 232);
        //}

        //internal static void DecryptPokemon(Pokemon pokemon)
        //{
        //    Array.Copy(decryptArray(pokemon.data), 0, pokemon.data, 0, 232);
        //}
    }
}
