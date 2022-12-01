using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;
using Syroot.BinaryData.Core;

using GTAdhocToolchain.Core;
using System.IO;

namespace GTAdhocToolchain.Core
{
    public class AdhocStream : BinaryStream
    {
        public int Version { get; set; }

        public List<AdhocSymbol> Symbols { get; set; } = new();

        static AdhocStream()
        {
            // Needed for EUC-JP encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public AdhocStream(Stream baseStream, int version)
            : base(baseStream)
        {
            Version = version;

            /* In GT4, the encoding for the compiler is set to EUC-JP
             * "ぐわあああ\n" in boot/CheckRoot.ad
             * 
             * So set the encoding to it if it's earlier than EUC-JP */
            if (Version < 10)
                Encoding = Encoding.GetEncoding("EUC-JP");
        }

        public void Reset()
        {
            Symbols.Clear();
        }

        public void ReadSymbolTable()
        {
            uint entryCount = (uint)DecodeBitsAndAdvance();
            Symbols = new List<AdhocSymbol>((int)entryCount);
            for (var i = 0; i < entryCount; i++)
            {
                int strLen = (int)DecodeBitsAndAdvance();

                // Bugged, doesnt actually read the string length
                //StringTable[i] = sr.ReadStringRaw(strLen);
                Symbols.Add(new AdhocSymbol(Encoding.GetString(ReadBytes(strLen))));
            }
        }

        public void WriteSymbols(IEnumerable<AdhocSymbol> symbols)
        {
            WriteInt32(symbols.Count());
            foreach (var symb in symbols)
                WriteSymbol(symb);
        }

        public void WriteSymbol(AdhocSymbol symbol)
        {
            if (Version <= 8)
            {
                WriteInt16((short)Encoding.GetByteCount(symbol.Name));
                WriteBytes(Encoding.GetBytes(symbol.Name));
            }
            else
                WriteVarInt(symbol.Id);
                    
        }

        public List<AdhocSymbol> ReadSymbols()
        {
            uint symbCount = ReadUInt32();
            List<AdhocSymbol> list = new List<AdhocSymbol>((int)symbCount);

            for (int i = 0; i < symbCount; i++)
            {
                 AdhocSymbol symbol = ReadSymbol();
                 list.Add(symbol);
            }

            return list;
        }

        public AdhocSymbol ReadSymbol()
        {
            if (Version >= 9)
            {
                uint symbolTableIdx = (uint)DecodeBitsAndAdvance();
                return Symbols[(int)symbolTableIdx];
            }
            else
            {
                // Reads more than it should with the following (bug):
                // length: 0x0B
                // - text: ¤°¤ï¤¢¤¢¤¢
                // - bytes: A4 B0 A4 EF A4 A2 A4 A2 A4 A2
                // var symbStr = this.ReadString(StringCoding.Int16CharCount);

                // Read manually
                short len = this.ReadInt16();
                var symbStr = Encoding.GetString(ReadBytes(len));

                return new AdhocSymbol(symbStr);
            }
        }

        public ulong DecodeBitsAndAdvance()
        {
            ulong value = (ulong)ReadByte();
            ulong mask = 0x80;

            while ((value & mask) != 0)
            {
                value = ((value - mask) << 8) | (Read1Byte());
                mask <<= 7;
            }
            return value;
        }

        public void WriteVarString(string str, bool asUtf8 = true)
        {
            // HACK: Hex sequences may not be only hex escapes, they may be incorrectly written
            // Best way to handle it for now (?) is just to treat anything with a hex escaped character as full ascii

            if (asUtf8)
            {
                // Must convert, has some utf8 chars, i.e japanese
                WriteVarInt(Encoding.UTF8.GetByteCount(str));
                StreamExtensions.WriteString(this, str, StringCoding.Raw);
            }
            else
            {
                // Non UTF8 operation, incase the string is a escaped byte array as string
                WriteVarInt(str.Length);
                byte[] data = new byte[str.Length];
                for (int i = 0; i < str.Length; i++)
                    data[i] = (byte)str[i];

                this.Write(data);
            }
        }

        public bool IsAscii(string str) 
        {
            for (int i = 0; i<str.Length; i++) 
            {
                if (str[i] < 0 || str[i] > 0xFF)
                    return false;
            }
            return true;
        }

        // Credits xfileFin
        public void WriteVarInt(int value)
        {
            if ((value & 0xFFFFFF80) == 0)
            {
                Write((byte)value);
                return;
            }

            var bytesToWrite = 1;
            uint mask = 0x80;
            long retVal = 0;
            do
            {
                retVal = (retVal + mask) << 8;
                mask <<= 7;
                bytesToWrite++;
            } while ((value & -mask) > 0);

            var finalValue = retVal | value;
            for (var i = bytesToWrite; i > 0; i--)
                Write((byte)(finalValue >> (i - 1) * 8));
        }

        public static byte[] EncodeAndAdvance(uint value)
        {
            uint mask = 0x80;
            Span<byte> buffer = Array.Empty<byte>();

            if (value <= 0x7F)
            {
                return new[] { (byte)value };
            }
            else if (value <= 0x3FFF)
            {
                Span<byte> tempBuf = BitConverter.GetBytes(value).AsSpan();
                tempBuf.Reverse();
                buffer = tempBuf.Slice(2, 2);
            }
            else if (value <= 0x1FFFFF)
            {
                Span<byte> tempBuf = BitConverter.GetBytes(value).AsSpan();
                tempBuf.Reverse();
                buffer = tempBuf.Slice(1, 3);
            }
            else if (value <= 0xFFFFFFF)
            {
                buffer = BitConverter.GetBytes(value);
                buffer.Reverse();
            }
            else if (value <= 0xFFFFFFFF)
            {
                buffer = BitConverter.GetBytes(value);
                buffer.Reverse();
                buffer = new byte[] { 0, buffer[0], buffer[1], buffer[2], buffer[3] };
            }
            else
                throw new Exception("????");

            for (int i = 1; i < buffer.Length; i++)
            {
                buffer[0] += (byte)mask;
                mask >>= 1;
            }

            return buffer.ToArray();
        }
    }
}
