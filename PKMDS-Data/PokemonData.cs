using System;
using System.Collections;

namespace PKMDSData
{
    public class PokemonData
    {
        private readonly byte[] RawData;

        public PokemonData(byte[] rawData) => RawData = rawData;

        private byte GetByteValue(int startIndex) => RawData[startIndex];
        private ushort GetShortValue(int startIndex) => BitConverter.ToUInt16(RawData, startIndex);
        private ulong GetLongValue(int startIndex) => BitConverter.ToUInt32(RawData, startIndex);

        private void SetValue(byte value, int destinationIndex) => Array.Copy(BitConverter.GetBytes(value), 0, RawData, destinationIndex, 1);
        private void SetValue(ushort value, int destinationIndex) => Array.Copy(BitConverter.GetBytes(value), 0, RawData, destinationIndex, 2);
        private void SetValue(ulong value, int destinationIndex) => Array.Copy(BitConverter.GetBytes(value), 0, RawData, destinationIndex, 4);

        public ulong Pid
        {
            get => GetLongValue(0x00);
            set => SetValue(value, 0x00);
        }

        public ushort NationalId
        {
            get => GetShortValue(0x08);
            set => SetValue(value, 0x08);
        }

        public ushort Tid
        {
            get => GetShortValue(0x0C);
            set => SetValue(value, 0x0C);
        }

        public ushort Sid
        {
            get => GetShortValue(0x0E);
            set => SetValue(value, 0x0E);
        }

        private byte RandomStuff
        {
            get => GetByteValue(0x40);
            set => SetValue(value, 0x40);
        }

        public bool IsShiny => (((ulong)(Tid ^ Sid)) ^ ((Pid & 0xFFFF0000U) >> 16) ^ (Pid & 0xFFFFU)) < 8U;

        public bool IsFatefulEncounter
        {
            get => new BitArray(new byte[] { RandomStuff })[0];
            set
            {
                BitArray ba = new BitArray(new byte[] { RandomStuff });
                ba.Set(0, value);
                ba.CopyTo(RawData, 0x40);
            }
        }

        public bool IsFemale
        {
            get => new BitArray(new byte[] { RandomStuff })[1];
            set
            {
                BitArray ba = new BitArray(new byte[] { RandomStuff });
                ba.Set(1, value);
                ba.CopyTo(RawData, 0x40);
            }
        }

        public bool IsGenderless
        {
            get => new BitArray(new byte[] { RandomStuff })[2];
            set
            {
                BitArray ba = new BitArray(new byte[] { RandomStuff });
                ba.Set(2, value);
                ba.CopyTo(RawData, 0x40);
            }
        }

        public byte FormeIndex
        {
            get
            {
                BitArray ba = new BitArray(new byte[] { RandomStuff });

                ba[7] = false;
                ba[6] = false;
                ba[5] = false;

                byte[] temp = new byte[] { 0 };
                ba.CopyTo(temp, 0);
                return temp[0];
            }
            set
            {
                BitArray ba = new BitArray(new byte[] { RandomStuff });
                BitArray tempBa = new BitArray(new byte[] { value });

                tempBa[7] = ba[7];
                tempBa[6] = ba[6];
                tempBa[5] = ba[5];

                tempBa.CopyTo(RawData, 0x40);
            }
        }
    }
}
