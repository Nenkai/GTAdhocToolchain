using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using Syroot.BinaryData;
using ICSharpCode.SharpZipLib.Zip.Compression;

using ICSharpCodeDeflater = ICSharpCode.SharpZipLib.Zip.Compression.Deflater;

namespace GTAdhocToolchain.Packaging
{
    public class AdhocPackage
    {
        public const string Magic = "MPKG";

        public List<AdhocPackageFile> Files { get; set; } = new();

        public static void ExtractPackage(string path)
        {
            using var fs = new FileStream(path, FileMode.Open);
            var stream = new BinaryStream(fs, encoding: Encoding.ASCII);
            if (stream.ReadString(4) != Magic)
                throw new Exception("Invalid MAGIC, doesn't match MPKG.");

            stream.Position += 4;

            uint fileEntryCount = stream.ReadUInt32();
            uint tableOfContentsOffset = stream.ReadUInt32(); // Table of contents

            string dir = Path.GetDirectoryName(path);
            string outDir = $"{Path.GetFileNameWithoutExtension(path)}_extracted";

            Directory.CreateDirectory(outDir);

            byte[] buffer = new byte[512_000];
            for (int i = 0; i < fileEntryCount; i++)
            {
                AdhocPackageFile file = new AdhocPackageFile();
                stream.Position = (int)tableOfContentsOffset + (i * (sizeof(uint) * 3));

                file.FileNameOffset = stream.ReadUInt32();
                file.CompressedDataOffset = stream.ReadUInt32();
                file.CompressedDataSize = stream.ReadUInt32();

                stream.Position = (int)file.FileNameOffset;
                file.RawFileName = stream.ReadString(StringCoding.ZeroTerminated);

                file.ProjectFileName = file.RawFileName.Replace("%P", "gt6");
                Directory.CreateDirectory(outDir + Path.GetDirectoryName(file.ProjectFileName));

                stream.Position = (int)file.CompressedDataOffset;
                byte[] compressed = new byte[(int)file.CompressedDataSize];
                stream.Read(compressed, 0, (int)compressed.Length);

                Inflater inflater = new Inflater(noHeader: true);
                inflater.SetInput(compressed, 0, (int)file.CompressedDataSize);
                int uncompressedSize = inflater.Inflate(buffer);

                using (var output = new FileStream(outDir + file.ProjectFileName, FileMode.Create))
                    output.Write(buffer, 0, uncompressedSize);

                Console.WriteLine($"Extracted: {file.RawFileName}");
                Array.Clear(buffer, 0, buffer.Length);
            }
        }

        public static void PackFromFolder(string inputFolder, string outFile)
        {
            // Relative to absolute
            inputFolder = Path.GetFullPath(inputFolder);

            Console.WriteLine("[:] Packing MPackage..");

            var files = Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories)
                .OrderBy(e => e, StringComparer.Ordinal)
                       .ToArray();

            if (outFile is null)
                outFile = $"{Path.GetFileName(inputFolder)}.mpackage";

            using var fs = new FileStream(outFile, FileMode.Create);
            using var bs = new BinaryStream(fs, ByteConverter.Little);

            bs.WriteString(Magic, StringCoding.Raw);
            bs.WriteInt32(0); // Relocation Ptr
            bs.WriteInt32(files.Length);

            // Skip toc offset for now
            bs.Position = 0x10;

            var folderNameStart = inputFolder.Length;

            List<(int stringOffset, int dataOffset, int compressedSize)> toc = new(files.Length);
            foreach (var file in files)
            {
                int stringOffset = (int)bs.Position;
                string fileName = file.Replace('\\', '/'); // Replace to any wanted path separator
                fileName = fileName.Substring(folderNameStart); // Remove the parent
                if (!fileName.StartsWith('/'))
                    fileName = '/' + fileName; // Ensure it starts with '/'

                fileName = fileName.Replace("gt6", "%P");
                Console.WriteLine($"[:] Adding {fileName}");

                bs.WriteString(fileName, StringCoding.ZeroTerminated);
                int entryOffsetPos = (int)bs.Position;
                using (var ds = new DeflateStream(bs, CompressionMode.Compress, true))
                {
                    byte[] fileData = File.ReadAllBytes(file);
                    ds.Write(fileData, 0, fileData.Length);
                }
                int compressSize = (int)bs.Position - entryOffsetPos;

                toc.Add( (stringOffset, entryOffsetPos, compressSize) );
            }

            bs.Align(0x04, true);
            int tocOffset = (int)bs.Position;

            for (int i = 0; i < toc.Count; i++)
            {
                bs.WriteInt32(toc[i].stringOffset);
                bs.WriteInt32(toc[i].dataOffset);
                bs.WriteInt32(toc[i].compressedSize);
            }

            bs.Position = 0x0C;
            bs.WriteInt32(tocOffset);
        }


    }
}
