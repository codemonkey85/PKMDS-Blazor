using PKMDSData;

PokemonData pokemon;
FileInfo saveFile;

saveFile = new FileInfo(@"Bulbasaur_Platinum(Platinum001).pkm");
pokemon = ReadSaveFile(saveFile);

Console.WriteLine(pokemon);

Console.WriteLine(pokemon.FormeIndex);

pokemon.FormeIndex++;

Console.WriteLine(pokemon.FormeIndex);

static PokemonData ReadSaveFile(FileInfo saveFile)
{
    using FileStream fileStream = new(saveFile.FullName, FileMode.Open, FileAccess.Read);
    using BinaryReader binaryReader = new(fileStream);
    Span<byte> fileBytes = new(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));
    binaryReader.Close();
    fileStream.Close();

    return new PokemonData(fileBytes.ToArray());
}
