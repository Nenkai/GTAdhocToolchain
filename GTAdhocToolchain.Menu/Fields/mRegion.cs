using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mRegion: {Name} ({X1},{Y1},{X2},{Y2})")]
    public class mRegion : mTypeBase
    {
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }

        public override void Read(MBinaryIO io)
        {
            if (io.Version == 0)
            {
                X1 = io.Stream.ReadSingle();
                Y1 = io.Stream.ReadSingle();
                X2 = io.Stream.ReadSingle();
                Y2 = io.Stream.ReadSingle();
            }
            else
            {
                mFloat x1 = io.ReadNext() as mFloat;
                mFloat y1 = io.ReadNext() as mFloat;
                mFloat x2 = io.ReadNext() as mFloat;
                mFloat y2 = io.ReadNext() as mFloat;

                X1 = x1.Value;
                Y1 = y1.Value;
                X2 = x2.Value;
                Y2 = y2.Value;
            }
        }

        public override void Read(MTextIO io)
        {
            var x1 = io.GetNumberToken();
            if (float.TryParse(x1, out float x1Val))
                X1 = x1Val;
            else
                throw new UISyntaxError($"Unexpected token for mRegion X1. Got {x1}.");

            var y1 = io.GetNumberToken();
            if (float.TryParse(y1, out float y1Val))
                Y1 = y1Val;
            else
                throw new UISyntaxError($"Unexpected token for mRegion Y1. Got {y1}.");

            var x2 = io.GetNumberToken();
            if (float.TryParse(x2, out float x2Val))
                X2 = x2Val;
            else
                throw new UISyntaxError($"Unexpected token for mRegion X2. Got {x2}.");

            var y2 = io.GetNumberToken();
            if (float.TryParse(y2, out float y2Val))
                Y2 = y2Val;
            else
                throw new UISyntaxError($"Unexpected token for mRegion Y2. Got {y2}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mRectangle scope end ({MTextIO.SCOPE_END}), got {end}");
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
                writer.Stream.WriteVarString("region");

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(X1);

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(Y1);

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(X2);

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(Y2);
            }
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("region");
            writer.WriteString("{"); writer.WriteString($"{X1} {Y1} {X2} {Y2}"); writer.WriteString("}");
            writer.SetNeedNewLine();
        }
    }
}
