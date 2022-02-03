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
    [DebuggerDisplay("mVector: {Name} ({X} {Y})")]
    public class mVector : mTypeBase
    {
        public float X { get; set; }
        public float Y { get; set; }

        public override void Read(MBinaryIO io)
        {
            if (io.Version == 0)
            {
                X = io.Stream.ReadSingle();
                Y = io.Stream.ReadSingle();
            }
            else
            {
                X = (io.ReadNext() as mFloat).Value;
                Y = (io.ReadNext() as mFloat).Value;
            }
        }

        public override void Read(MTextIO io)
        {
            var x = io.GetNumberToken();
            if (float.TryParse(x, out float xVal))
                X = xVal;
            else
                throw new UISyntaxError($"Unexpected token for mVector X. Got {x}.");

            var y = io.GetNumberToken();
            if (float.TryParse(y, out float yVal))
                Y = yVal;
            else
                throw new UISyntaxError($"Unexpected token for mVector Y. Got {x}.");


            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mVector scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            if (writer.Version == 0)
                throw new NotImplementedException();
            else
            {
                writer.Stream.WriteVarInt((int)FieldType.String);
                writer.Stream.WriteVarString("vector");

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(X);

                writer.Stream.WriteVarInt((int)FieldType.Float);
                writer.Stream.WriteSingle(Y);
            }
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("vector");
            writer.WriteString("{"); writer.Write(X); writer.WriteSpace(); writer.Write(Y); writer.WriteString("}");
            writer.SetNeedNewLine();
        }
    }
}
