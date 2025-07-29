using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using Syroot.BinaryData;
using Syroot.BinaryData.Memory;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Resources;

/// <summary>
/// Gpb3, used in PS3 GTs.
/// </summary>
public class GpbData3 : GpbBase
{
    public const int HeaderSize = 0x20;
    public const int EntrySize = 0x10;

    public override void Read(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Open);
        using var bs = new BinaryStream(fs, ByteConverter.Big);

        string magic = bs.ReadString(4);
        if (magic == "3bpg")
            bs.ByteConverter = ByteConverter.Big;
        else if (magic == "gpb3")
            bs.ByteConverter = ByteConverter.Little;
        else
            throw new Exception($"Unsupported gpb with magic {magic}.");

        bs.ReadInt32(); // Relocation ptr
        int headerSize = bs.ReadInt32(); // Empty
        int entryCount = bs.ReadInt32();

        int entriesOffset = bs.ReadInt32();
        int fileNamesOffset = bs.ReadInt32();
        int filesDataOffset = bs.ReadInt32();

        for (int i = 0; i < entryCount; i++)
        {
            bs.Position = entriesOffset + (i * EntrySize);

            int fileNameOffset = bs.ReadInt32();
            int fileDataOffset = bs.ReadInt32();
            int fileSize = bs.ReadInt32();

            bs.Position = fileNameOffset;
            var file = new GpbPair();
            file.FileName = bs.ReadString(StringCoding.ZeroTerminated);

            bs.Position = fileDataOffset;
            file.FileData = bs.ReadBytes(fileSize);
            Files.Add(file);
        }
    }        

    public void Pack(string outputFileName, bool bigEndian = true)
    {
        Console.WriteLine($"[:] GPB: Packing {Files.Count} to -> {outputFileName}..");

        using var fs = new FileStream(outputFileName, FileMode.Create);
        using var bs = new BinaryStream(fs, bigEndian ? ByteConverter.Big : ByteConverter.Little);

        if (bigEndian)
            bs.WriteString("3bpg", StringCoding.Raw);
        else
            bs.WriteString("gpb3", StringCoding.Raw);

        bs.WriteInt32(0);
        bs.WriteInt32(HeaderSize); // Header Size
        bs.WriteInt32(Files.Count);

        // Write all file names first
        int baseFileNameOffset = HeaderSize + (EntrySize * Files.Count);
        int currentFileNameOffset = baseFileNameOffset;

        // Game uses bsearch, important
        Files.Sort(GpbBase.PairSorter);

        for (int i = 0; i < Files.Count; i++)
        {
            bs.Position = HeaderSize + (i * EntrySize);
            bs.WriteInt32(currentFileNameOffset);

            bs.Position = currentFileNameOffset;
            bs.WriteString(Files[i].FileName, StringCoding.ZeroTerminated);
            currentFileNameOffset = (int)bs.Position;
        }

        bs.AlignWithValue(0x80, 0x5E, true);
        int baseDataOffset = (int)bs.Position; // Align with 0x5E todo
        int currentFileDataOffset = baseDataOffset;

        // Write the buffers
        for (int i = 0; i < Files.Count; i++)
        {
            bs.Position = HeaderSize + (i * EntrySize) + 4;
            bs.WriteInt32(currentFileDataOffset);
            bs.WriteInt32(Files[i].FileData.Length);

            bs.Position = currentFileDataOffset;
            bs.WriteBytes(Files[i].FileData);

            bs.AlignWithValue(0x80, 0x5E, true);
            currentFileDataOffset = (int)bs.Position;
        }

        bs.Position = 0x10;
        bs.WriteInt32(HeaderSize); // Offset of entries
        bs.WriteInt32(baseFileNameOffset);
        bs.WriteInt32(baseDataOffset);

        Console.WriteLine($"[:] GPB: Done packing {Files.Count} files to {outputFileName}.");
    }
}
