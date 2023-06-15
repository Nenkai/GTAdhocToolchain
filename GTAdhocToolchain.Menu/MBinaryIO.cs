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

namespace GTAdhocToolchain.Menu
{
    public class MBinaryIO
    {
        public string FileName { get; set; }
        public BinaryStream Stream { get; set; }
        public byte Version { get; set; }

        public string CurrentKeyName { get; set; }

        public MBinaryIO(string fileName)
        {
            FileName = fileName;
        }

        public mNode Read()
        {
            using var file = File.Open(FileName, FileMode.Open);
            Stream = new BinaryStream(file, ByteConverter.Big);

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
            if (Version != 0 && Version != 1)
            {
                Console.WriteLine($"Unsupported MPRJ Version {Version}.");
                return null;
            }

            var rootPrjNode = new mNode();
            rootPrjNode.IsRoot = true; // For version 0
            if (Version == 1)
                Stream.Position += 1; // Skip scope type

            Console.WriteLine($"MPRJ Version: {Version}");
            rootPrjNode.Read(this);

            Stream.Dispose();

            return rootPrjNode;
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
