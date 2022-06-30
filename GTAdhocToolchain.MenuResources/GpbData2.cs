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

namespace GTAdhocToolchain.Menu.Resources
{
    public class GpbData2 : GpbBase
    {
        public const int HeaderSize = 0x10;
        public const int EntrySize = 0x10;

        public override void Read(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Big);

            string magic = bs.ReadString(4);
            if (magic == "2bpg")
                bs.ByteConverter = ByteConverter.Big;
            else if (magic == "gpb2")
                bs.ByteConverter = ByteConverter.Little;
            else
                throw new Exception($"Unsupported gpb with magic {magic}.");

            bs.ReadInt32(); // Relocation ptr
            int headerSize = bs.ReadInt32(); // Empty
            int entryCount = bs.ReadInt32();

            for (int i = 0; i < entryCount; i++)
            {
                bs.Position = HeaderSize + (i * EntrySize);

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
                bs.WriteString("2bpg", StringCoding.Raw);
            else
                bs.WriteString("gpb2", StringCoding.Raw);

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

            Console.WriteLine($"[:] GPB: Done packing {Files.Count} files to {outputFileName}.");
        }
    }
}
