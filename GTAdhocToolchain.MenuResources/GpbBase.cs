using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;

using Syroot.BinaryData;

using PDTools.Files.Textures;
using PDTools.Compression;
using PDTools.Files.Textures.PS2;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Resources;

public abstract class GpbBase
{
    public List<GpbPair> Files { get; set; } = [];

    public abstract void Read(string fileName);

    public static GpbBase ReadFile(string file)
    {
        GpbBase gpb;
        using (var fs = new FileStream(file, FileMode.Open))
        using (var bs = new BinaryStream(fs)) 
        {
            string magic = bs.ReadString(4);

            gpb = magic switch
            {
                "2bpg" or "gpb2" => new GpbData2(),
                "3bpg" or "gpb3" => new GpbData3(),
                "4bpg" or "gpb4" => new GpbData4(),
                _ => null,
            };
        }

        if (gpb is null)
            return null;

        gpb.Read(file);
        return gpb;
    }

    public void Unpack(string gpbName, string outputFolder, bool convertImages = true)
    {
        foreach (var file in Files)
        {
            Console.WriteLine($"[:] GPB: Unpack -> {file.FileName}");

            string path;
            if (this is GpbData2)
                path = file.FileName;
            else
                path = Path.Combine(file.FileName.Substring(1));

            string outputFile = Path.Combine(outputFolder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            if (BinaryPrimitives.ReadUInt32LittleEndian(file.FileData) == PS2ZIP.PS2ZIP_MAGIC)
                file.FileData = PS2ZIP.Inflate(file.FileData);

            if (convertImages && this is GpbData4 && BinaryPrimitives.ReadUInt32LittleEndian(file.FileData) == 0x30504449) // Is GTS/7 gpb, is PDI0 (GT7), and allow converting to png
            {
                using var ms = new MemoryStream(file.FileData);
                PDITexture pdiTexture = new PDITexture();
                pdiTexture.FromStream(ms);

                if (Path.GetExtension(outputFile) == ".psd")
                    outputFile += ".png"; // We obviously don't support converting to... psd

                Console.WriteLine($"[:] GPB: 0PDI to {Path.GetExtension(outputFile)} -> {file.FileName}");

                pdiTexture.TextureSet.ConvertToStandardFormat(outputFile);
            }
            else if (convertImages && this is GpbData3 && BinaryPrimitives.ReadUInt32LittleEndian(file.FileData) == 0x33535854) // PS3
            {
                Console.WriteLine($"[:] GPB: TXS to {Path.GetExtension(file.FileName)} -> {file.FileName}");

                using var ms = new MemoryStream(file.FileData);
                TextureSet3 texSet = new TextureSet3();
                texSet.FromStream(ms, TextureSet3.TextureConsoleType.PS3);
                texSet.ConvertToStandardFormat(Path.ChangeExtension(outputFile, ".png"));
            }
            else if (convertImages && this is GpbData2 && BinaryPrimitives.ReadUInt32LittleEndian(file.FileData) == TextureSet1.MAGIC) // PS2
            {
                Console.WriteLine($"[:] GPB: Tex1 to {Path.GetExtension(file.FileName)} -> {file.FileName}");

                using var ms = new MemoryStream(file.FileData);
                TextureSet1 texSet = new TextureSet1();
                texSet.FromStream(ms);

                for (int i = 0; i < texSet.pgluTextures.Count; i++)
                {
                    using Image<Rgba32> image = texSet.GetTextureImage(i);
                    image.Save(outputFile);
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(outputFolder, Path.GetDirectoryName(path)));
                File.WriteAllBytes(outputFile, file.FileData);
            }
        }
    }

    public void AddFilesFromFolder(string folderName, bool gt5 = false)
    {
        List<string> files = Directory.GetFiles(folderName, "*", SearchOption.AllDirectories).ToList();
        files.Sort(Utils.AlphaNumericStringSorter);

        foreach (var file in files)
        {
            var pair = new GpbPair();

            string fileName = file.Replace('\\', '/'); // Replace to any wanted path separator
            fileName = fileName.Substring(fileName.IndexOf('/')); // Remove the parent
            if (!gt5)
            {
                if (!fileName.StartsWith('/'))
                    fileName = '/' + fileName; // Ensure it starts with '/' if not GT5
            }
            else
            {
                if (fileName.StartsWith('/'))
                    fileName = fileName.Substring(1); // Ensure it DOESN'T start with '/' if GT5
            }

            pair.FileName = fileName;
            pair.FileData = File.ReadAllBytes(file);
            Files.Add(pair);
        }
    }

    public static int PairSorter(GpbPair a, GpbPair b)
    {
        return Utils.AlphaNumericStringSorter(a.FileName, b.FileName);
    }
}
