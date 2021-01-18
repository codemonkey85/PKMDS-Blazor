using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PKMDSData;

namespace PKMDSBlazor.Components
{
    public partial class SaveFileComponent : ComponentBase
    {
        public string SaveFileName { get; set; }

        public byte PartySize { get; set; }

        public List<ushort> PartyPokemon { get; set; } = new List<ushort>();

        public PokemonData Pokemon { get; set; } = null;

        protected PokemonComponent PokemonComponent;

        IBrowserFile inputFile;
        FileInfo saveFile;

        private void ReadSaveFile()
        {
            if (!(saveFile?.Exists ?? false))
            {
                return;
            }

            Pokemon = null;

            using FileStream fileStream = new FileStream(saveFile.FullName, FileMode.Open, FileAccess.Read);
            using BinaryReader binaryReader = new BinaryReader(fileStream);
            Span<byte> fileBytes = new Span<byte>(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));
            binaryReader.Close();
            fileStream.Close();

            Pokemon = new PokemonData(fileBytes.ToArray());

            //SaveFileName = ConvertDsToUnicode(fileBytes.Slice(0x64, 16));

            //PartySize = Convert.ToByte(fileBytes[0x94]); // BitConverter.ToUInt16(fileBytes.Slice(0x94, 1));
            //Span<byte> partyPkm = fileBytes.Slice(0x98, 236 * PartySize);

            //PartyPokemon.Clear();
            //for (int i = 0; i < PartySize; i++)
            //{
            //    byte[] ekx = partyPkm.Slice(i * 236, 236).ToArray();
            //    PokePrng.ShuffleArray(ref ekx);
            //    PartyPokemon.Add(BitConverter.ToUInt16(ekx, 0x08));
            //}
        }

        private static string ConvertDsToUnicode(Span<byte> textBytes) => ConvertDsToUnicode(textBytes.ToArray());

        private static string ConvertDsToUnicode(byte[] textBytes)
        {
            List<ushort> textUints = new List<ushort>();
            for (int i = 0; i < textBytes.Length; i += 2)
            {
                textUints.Add(BitConverter.ToUInt16(textBytes, i));
            }

            StringBuilder dsToUnicodeStringBuilder = new StringBuilder();
            foreach (ushort textUint in textUints)
            {
                if (textUint == 0xFFFF)
                {
                    break;
                }
                TextConversion.DsToUnicode.TryGetValue(textUint, out int unicodeValue);
                dsToUnicodeStringBuilder.Append(Encoding.Unicode.GetString(BitConverter.GetBytes(unicodeValue)));
            }
            return dsToUnicodeStringBuilder.ToString();
        }

        private async void OnSubmit()
        {
            Stream stream = inputFile.OpenReadStream(524288);
            string path = $"{inputFile.Name}";
            FileStream fs = File.Create(path);
            await stream.CopyToAsync(fs);
            stream.Close();
            fs.Close();

            saveFile = new FileInfo(path);

            ReadSaveFile();

            StateHasChanged();
        }

        private async Task Refresh()
        {
            StateHasChanged();
            await PokemonComponent.Refresh();
        }

        public void OnInputFileChange(InputFileChangeEventArgs e) => inputFile = e.File;
    }
}
