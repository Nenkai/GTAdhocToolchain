using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;
using Syroot.BinaryData.Memory;

namespace GTAdhocToolchain.Core
{
    public static class Utils
    {
        public static string OperatorNameToPunctuator(string @operator)
        {
            if (string.IsNullOrEmpty(@operator))
                throw new Exception("Tried to call GetHumanReadable while name was null. Did you even call deserialize?");

            switch (@operator)
            {
                case "__elem__":
                    return "[]";

                case "__eq__":
                    return "==";
                case "__ge__":
                    return ">=";
                case "__gt__":
                    return ">";
                case "__le__":
                    return "<=";
                case "__lt__":
                    return "<";

                case "__invert__":
                    return "~";
                case "__lshift__":
                    return "<<";

                case "__mod__":
                    return "%";

                case "__ne__":
                    return "!=";

                case "__not__":
                    return "!";

                case "__or__":
                    return "|";

                case "__post_decr__":
                    return "@--";
                case "__post_incr__":
                    return "@++";

                case "__pre_decr__":
                    return "--@";
                case "__pre_incr__":
                    return "++@";

                case "__pow__":
                    return "** (power)";

                case "__rshift__":
                    return ">>";

                case "__minus__":
                    return "-";

                case "__uminus__":
                    return "-@";

                case "__uplus__":
                    return "+@";

                case "__xor__":
                    return "^";

                case "__div__":
                    return "/";
                case "__mul__":
                    return "*";
                case "__add__":
                    return "+";
                case "__min__":
                    return "-";


                default:
                    return @operator;
            }
        }

        public static string Read7BitString(this BinaryStream sr)
        {
            ulong strLen = DecodeBitsAndAdvance(sr);
            return Encoding.UTF8.GetString(sr.ReadBytes((int)strLen));
        }

        public static byte[] Read7BitStringBytes(this BinaryStream sr)
        {
            ulong strLen = DecodeBitsAndAdvance(sr);
            return sr.ReadBytes((int)strLen);
        }

        public static void WriteVarInt(this BinaryStream bs, int val)
        {
            Span<byte> buffer = Array.Empty<byte>();

            if (val <= 0x7F)
            {
                bs.WriteByte((byte)val);
                return;
            }
            else if (val <= 0x3FFF)
            {
                Span<byte> tempBuf = BitConverter.GetBytes(val).AsSpan();
                tempBuf.Reverse();
                buffer = tempBuf.Slice(2, 2);
            }
            else if (val <= 0x1FFFFF)
            {
                Span<byte> tempBuf = BitConverter.GetBytes(val).AsSpan();
                tempBuf.Reverse();
                buffer = tempBuf.Slice(1, 3);
            }
            else if (val <= 0xFFFFFFF)
            {
                buffer = BitConverter.GetBytes(val).AsSpan();
                buffer.Reverse();
            }
            else if (val <= 0xFFFFFFFF)
            {
                buffer = BitConverter.GetBytes(val);
                buffer.Reverse();
                buffer = new byte[] { 0, buffer[0], buffer[1], buffer[2], buffer[3] };
            }

            uint mask = 0x80;
            for (int i = 1; i < buffer.Length; i++)
            {
                buffer[0] += (byte)mask;
                mask >>= 1;
            }

            bs.Write(buffer);
        }

        public static void WriteVarString(this BinaryStream bs, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteVarInt(bs, bytes.Length);
            bs.Write(bytes);
        }

        public static ulong DecodeBitsAndAdvance(this BinaryStream sr)
        {
            ulong value = (ulong)sr.ReadByte();
            ulong mask = 0x80;

            while ((value & mask) != 0)
            {
                value = ((value - mask) << 8) | (sr.Read1Byte());
                mask <<= 7;
            }
            return value;
        }

        public static ulong DecodeBitsAndAdvance(this ref SpanReader sr)
        {
            ulong value = sr.ReadByte();
            ulong mask = 0x80;

            while ((value & mask) != 0)
            {
                value = ((value - mask) << 8) | (sr.ReadByte());
                mask <<= 7;
            }
            return value;
        }

        public static void AlignWithValue(this BinaryStream bs, int alignment, byte value, bool grow = false)
        {
            long basePos = bs.Position;
            long newPos = bs.Align(alignment);

            bs.Position = basePos;
            for (long i = basePos; i < newPos; i++)
                bs.WriteByte(value);
        }
    }
}
