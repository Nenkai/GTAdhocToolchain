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

        public static string Read7BitString(this AdhocStream sr)
        {
            ulong strLen = DecodeBitsAndAdvance(sr);
            return Encoding.UTF8.GetString(sr.ReadBytes((int)strLen));
        }

        public static byte[] Read7BitStringBytes(this AdhocStream sr)
        {
            ulong strLen = DecodeBitsAndAdvance(sr);
            return sr.ReadBytes((int)strLen);
        }

        public static void WriteVarString(this AdhocStream bs, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            bs.WriteVarInt(bytes.Length);
            bs.Write(bytes);
        }

        public static ulong DecodeBitsAndAdvance(this AdhocStream sr)
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

        public static int AlphaNumericStringSorter(string v1, string v2)
        {
            int min = v1.Length > v2.Length ? v2.Length : v1.Length;
            for (int i = 0; i < min; i++)
            {
                if (v1[i] < v2[i])
                    return -1;
                else if (v1[i] > v2[i])
                    return 1;
            }
            if (v1.Length < v2.Length)
                return -1;
            else if (v1.Length > v2.Length)
                return 1;

            return 0;
        }
    }
}
