using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData.Core;
using Syroot.BinaryData;
using Syroot.BinaryData.Memory;

using GTAdhocToolchain.Menu.Fields;
using GTAdhocToolchain.Core;
using System.Diagnostics;
using System.Security.Cryptography;

namespace GTAdhocToolchain.Menu
{
    public class MBinaryIO
    {
        public string FileName { get; set; }
        public AdhocStream Stream { get; set; } // Fine to reuse
        public byte Version { get; set; }

        public string CurrentKeyName { get; set; }

        public MBinaryIO(string fileName)
        {
            FileName = fileName;
        }

        public mNode Read()
        {
            using var file = File.Open(FileName, FileMode.Open);
            Stream = new AdhocStream(file, 1) { BigEndian = true };

            string magic = Stream.ReadString(4);
            if (magic == "Proj")
            {
                // Text version
                return null;
            }
            else if (magic != "MPRJ")
            {
                Console.WriteLine($"Not a MPRJ Binary file.");
                return null;
            }

            Version = (byte)Stream.DecodeBitsAndAdvance();
            if (Version > 2)
            {
                Console.WriteLine($"Unsupported MPRJ Version {Version}.");
                return null;
            }

            if (Version >= 2)
            {
                byte[] md5Hash = Stream.ReadBytes(0x10);

                // Check MD5
                byte[] fileBytes = new byte[Stream.Length - Stream.Position];

                long tempPos = Stream.Position;
                Stream.ReadExactly(fileBytes);
                Stream.Position = tempPos;

                Stream.InitScrambler(md5Hash);
                Stream.ChachaScramblerState.DecryptBytes(fileBytes, fileBytes.Length, 0);
                if (!MD5.HashData(fileBytes).AsSpan().SequenceEqual(md5Hash))
                    Console.WriteLine("Warning: mproject md5 hash did not match expected");
            }

            var rootPrjNode = new mNode();
            rootPrjNode.IsRoot = true; // For version 0
            if (Version >= 2)
                Stream.Position += 1; // Skip scope type

            Console.WriteLine($"MPRJ Version: {Version}");
            rootPrjNode.Read(this);

            Debug.Assert(Stream.Position == Stream.Length);

            Stream.Dispose();

            return rootPrjNode;
        }

        public void PrintRootsGT7()
        {
            using StreamWriter sw = new StreamWriter("gpbs.txt");

            foreach (var r in Directory.GetDirectories(FileName))
            {
                string projName = Path.GetFileNameWithoutExtension(r);
                string projProj = Path.Combine(r, projName) + ".mproject";

                if (!File.Exists(projProj))
                    continue;

                using var file = File.Open(projProj, FileMode.Open);
                Stream = new AdhocStream(file, 1) { BigEndian = true };

                string magic = Stream.ReadString(4);
                if (magic != "MPRJ")
                {
                    Console.WriteLine($"${projProj}: Not a MPRJ Binary file.");
                    continue;
                }

                Version = (byte)Stream.DecodeBitsAndAdvance();
                if (Version > 2)
                {
                    Console.WriteLine($"{projProj}: Unsupported MPRJ Version {Version}.");
                    continue;
                }

                if (Version >= 2)
                {
                    byte[] md5Hash = Stream.ReadBytes(0x10);
                    Stream.InitScrambler(md5Hash);
                }

                var rootPrjNode = new mNode();
                rootPrjNode.IsRoot = true; // For version 0
                if (Version == 1)
                    Stream.Position += 1; // Skip scope type

                rootPrjNode.Read(this);

                Stream.Dispose();

                sw.WriteLine($"// Project: {projName}");
                foreach (var m in mNode.mStrings)
                {
                    sw.WriteLine($"projects/gt7/{projName}/gpb/{m}_4k.gpb");
                    sw.WriteLine($"projects/gt7/{projName}/gpb/{m}.gpb");
                }
                sw.WriteLine();

                mNode.mStrings.Clear();
            }

            /*
            using StreamWriter sw2 = new StreamWriter("strings.txt");
            foreach (var m in mNode.mStrings2.Distinct().OrderBy(e => e))
            {
                sw2.WriteLine(m);
            }
            */

            Console.WriteLine("Done.");
        }

        public mTypeBase ReadNext()
            => Read(false);

        public FieldType PeekType()
        {
            FieldType type = (FieldType)Stream.Read1Byte();
            Stream.Position -= 1;
            return type;
        }

        public mTypeBase Read(bool arrayElement = false)
        {
            if (Version == 0)
                throw new NotSupportedException("Unsupported for MPRJ version 1");
            else
                return ReadNew();
        }

        private mTypeBase ReadNew()
        {
            mTypeBase field = null;
            FieldType typeNew = (FieldType)Stream.DecodeBitsAndAdvance();

            field = typeNew switch
            {
                FieldType.UByte => new mUByte(),
                FieldType.Bool => new mBool(),
                FieldType.Short => new mShort(),
                FieldType.UShort => new mUShort(),
                FieldType.Float => new mFloat(),
                FieldType.ULong => new mULong(),
                FieldType.Long => new mLong(),
                FieldType.Int => new mInt(),
                FieldType.UInt => new mUInt(),
                FieldType.String => new mString(),
                FieldType.ArrayMaybe => new mArray(),
                FieldType.ScopeStart => new mNode(),
                FieldType.ScopeEnd => null,
                FieldType.SByte => new mSByte(),
                FieldType.ExternalRef => new mExternalRef(),
                _ => throw new Exception($"Type: {typeNew} not supported"),
            };

            if (typeNew != FieldType.ScopeEnd)
                field.Read(this);

            return field;
        }
    }

    public enum FieldType
    {
        Bool = 1,
        SByte = 2,
        Short = 3,
        Int = 4,
        Long = 5,
        UByte = 6,
        UShort = 7,
        UInt = 8,
        Float = 10,
        ULong = 11,
        String = 12,
        ArrayMaybe = 13,
        ScopeStart = 14,
        ScopeEnd = 15,
        ExternalRef = 16,
    }

    public enum FieldTypeOld
    {
        Bool = 0x80,
        SByte = 0x81,
        Short = 0x82,
        Int = 0x83,
        Long = 0x84,
        Byte = 0x85,
        UShort = 0x86,
        UInt = 0x87,
        ULong = 0x88,
        Float = 0x89,
        Double = 0x8A,
        String = 0x8B,
        Array = 0x8C,
        ScopeEnd = 0x8D,
    }
}
