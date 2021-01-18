using System;
using System.IO;
using PKMDSData;

namespace PKMDS_Test_App
{
    class Program
    {
        private static PokemonData Pokemon { get; set; } = null;

        private static FileInfo saveFile;

        static void Main(string[] args)
        {
            saveFile = new FileInfo(@"Bulbasaur_Platinum(Platinum001).pkm");
            ReadSaveFile();

            Console.WriteLine(Pokemon.FormeIndex);

            Pokemon.FormeIndex++;

            Console.WriteLine(Pokemon.FormeIndex);
        }

        private static void ReadSaveFile()
        {
            using FileStream fileStream = new FileStream(saveFile.FullName, FileMode.Open, FileAccess.Read);
            using BinaryReader binaryReader = new BinaryReader(fileStream);
            Span<byte> fileBytes = new Span<byte>(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));
            binaryReader.Close();
            fileStream.Close();

            Pokemon = new PokemonData(fileBytes.ToArray());
        }
    }
}
