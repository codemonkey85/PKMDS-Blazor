using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PKMDSData;
using System.Text;

namespace PKMDSBlazor.Components;

public partial class SaveFileComponent : ComponentBase
{
    public string SaveFileName { get; set; }

    public byte PartySize { get; set; }

    public List<PokemonData> PartyPokemon { get; set; } = new List<PokemonData>();

    protected PokemonComponent PokemonComponent;

    IBrowserFile inputFile;
    FileInfo saveFile;

    private void ReadSaveFile()
    {
        if (!(saveFile?.Exists ?? false))
        {
            return;
        }

        PartyPokemon.Clear();

        using FileStream fileStream = new(saveFile.FullName, FileMode.Open, FileAccess.Read);
        using BinaryReader binaryReader = new(fileStream);
        Span<byte> fileBytes = new(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));
        binaryReader.Close();
        fileStream.Close();

        //for (ushort i = 0; i < 6; i++)
        //{
        //    PokemonData pkm = new(fileBytes.ToArray());
        //    pkm.NationalId += i;
        //    PartyPokemon.Add(pkm);
        //}

        SaveFileName = ConvertDsToUnicode(fileBytes.Slice(0x64, 16));

        PartySize = Convert.ToByte(fileBytes[0x94]); // BitConverter.ToUInt16(fileBytes.Slice(0x94, 1));
        Span<byte> partyPkm = fileBytes.Slice(0x98, 236 * PartySize);

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
        List<ushort> textUints = new();
        for (int i = 0; i < textBytes.Length; i += 2)
        {
            textUints.Add(BitConverter.ToUInt16(textBytes, i));
        }

        StringBuilder dsToUnicodeStringBuilder = new();
        foreach (ushort textUint in textUints)
        {
            if (textUint == 0xFFFF)
            {
                break;
            }
            TextConversion.DsToUnicode.TryGetValue(textUint, out int unicodeValue);
            var dsStringBytes = BitConverter.GetBytes(unicodeValue);
            var dsString = Encoding.Unicode.GetString(dsStringBytes);
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
