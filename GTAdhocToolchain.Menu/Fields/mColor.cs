using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    public class mColor : mTypeBase
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public override void Read(MBinaryIO io)
        {
            if (io.Version == 0)
            {
                R = io.Stream.Read1Byte();
                G = io.Stream.Read1Byte();
                B = io.Stream.Read1Byte();
                A = io.Stream.Read1Byte();
            }
            else
            {
                R = (io.ReadNext() as mUByte).Value;
                G = (io.ReadNext() as mUByte).Value;
                B = (io.ReadNext() as mUByte).Value;
                A = (io.ReadNext() as mUByte).Value;
            }
        }

        public override void Read(MTextIO io)
        {
            var r = io.GetNumberToken();
            if (byte.TryParse(r, out byte rVal))
                R = rVal;
            else
                throw new UISyntaxError($"Unexpected token for mColor R. Got {r}.");

            var g = io.GetNumberToken();
            if (byte.TryParse(g, out byte gVal))
                G = gVal;
            else
                throw new UISyntaxError($"Unexpected token for mColor G. Got {g}.");

            var b = io.GetNumberToken();
            if (byte.TryParse(b, out byte bVal))
                B = bVal;
            else
                throw new UISyntaxError($"Unexpected token for mColor B. Got {b}.");

            var a = io.GetNumberToken();
            if (byte.TryParse(a, out byte aVal))
                A = aVal;
            else
                throw new UISyntaxError($"Unexpected token for mColor A. Got {a}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mColor scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            if (writer.Version == 0)
            {
                throw new NotImplementedException();
            }
            else 
            {
                writer.Stream.WriteVarInt((int)FieldType.String);
                writer.Stream.WriteVarString("RGBA");

                writer.Stream.WriteVarInt((int)FieldType.UByte);
                writer.Stream.WriteByte(R);

                writer.Stream.WriteVarInt((int)FieldType.UByte);
                writer.Stream.WriteByte(G);

                writer.Stream.WriteVarInt((int)FieldType.UByte);
                writer.Stream.WriteByte(B);

                writer.Stream.WriteVarInt((int)FieldType.UByte);
                writer.Stream.WriteByte(A);
            }
        }

        public override void WriteText(MTextWriter writer)
        {
            if (Name != null)
            {
                writer.WriteString(Name);
                writer.WriteSpace();
            }

            writer.WriteString("RGBA");
            writer.WriteString("{"); writer.WriteString($"{R} {G} {B} {A}"); writer.WriteString("}");
            writer.SetNeedNewLine();
        }
    }
}
