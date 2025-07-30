using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using Syroot.BinaryData;
using Syroot.BinaryData.Memory;

namespace GTAdhocToolchain.Menu.Resources;

/// <summary>
/// Gpb4, used in PS4/5 GTs.
/// </summary>
public class GpbData4 : GpbBase
{
    public const int HeaderSize = 0x20;
    public const int EntrySize = 0x20;

    public override void Read(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Open);
        using var bs = new BinaryStream(fs, ByteConverter.Big);

        string magic = bs.ReadString(4);
        if (magic == "4bpg")
            bs.ByteConverter = ByteConverter.Little;
        else if (magic == "gpb4")
            bs.ByteConverter = ByteConverter.Big;
        else
            throw new Exception($"Unsupported gpb with magic {magic}.");

        int headerSize = bs.ReadInt32();
        bs.Position = 0x10;

        int entryCount = bs.ReadInt32();

        int entriesOffset = bs.ReadInt32();
        int fileNamesOffset = bs.ReadInt32();
        int fileDataOffset = bs.ReadInt32();

        for (int i = 0; i < entryCount; i++)
        {
            bs.Position = entriesOffset + (i * EntrySize);
            long entryNameOffset = bs.ReadInt64();
            long dataOffset = bs.ReadInt64();
            bs.Position += 0x08;
            long fileSize = bs.ReadInt64();

            var file = new GpbPair();
            bs.Position = entryNameOffset;
            file.FileName = bs.ReadString(StringCoding.ZeroTerminated);

            bs.Position = dataOffset;
            file.FileData = bs.ReadBytes((int)fileSize);

            Files.Add(file);
        }
    }
}
