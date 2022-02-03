using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mVector3: {Name} ({X} {Y} {Z})")]
    public class mVector3 : mTypeBase
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override void Read(MBinaryIO io)
        {
            if (io.Version == 0)
            {
                X = io.Stream.ReadSingle();
                Y = io.Stream.ReadSingle();
                Z = io.Stream.ReadSingle();
            }
            else
            {
                X = (io.ReadNext() as mFloat).Value;
                Y = (io.ReadNext() as mFloat).Value;
                Z = (io.ReadNext() as mFloat).Value;
            }
        }

        public override void Read(MTextIO io)
        {
            var x = io.GetNumberToken();
            if (float.TryParse(x, out float xVal))
                X = xVal;
            else
                throw new UISyntaxError($"Unexpected token for mVector3 X. Got {x}.");

            var y = io.GetNumberToken();
            if (float.TryParse(y, out float yVal))
                Y = yVal;
            else
                throw new UISyntaxError($"Unexpected token for mVector3 Y. Got {x}.");

            var z = io.GetNumberToken();
            if (float.TryParse(z, out float zVal))
                Z = zVal;
            else
                throw new UISyntaxError($"Unexpected token for mVector3 Z. Got {z}.");


            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mVector3 scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            if (writer.Version == 0)
                throw new NotImplementedException();
            else
            {
                writer.Stream.WriteVarInt((int)FieldType.String);
                writer.Stream.WriteVarString("vector3");

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(X);

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(Y);

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(Z);
            }
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("vector3");
            writer.WriteString("{"); writer.Write(X); writer.WriteSpace(); writer.Write(Y); writer.WriteSpace(); writer.Write(Z); writer.WriteString("}");
            writer.SetNeedNewLine();
        }
    }
}
