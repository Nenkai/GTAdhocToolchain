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
    [DebuggerDisplay("mFloat: {Name} ({Value})")]
    public class mFloat : mTypeBase
    {
        public float Value { get; set; }

        public override void Read(MBinaryIO io)
        {
            Value = io.Stream.ReadSingle();
        }

        public override void Read(MTextIO io)
        {
            var numbToken = io.GetNumberToken();
            if (float.TryParse(numbToken, out float val))
                Value = val;
            else
                throw new UISyntaxError($"Unexpected token for mFloat. Got {numbToken}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mFloat scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.Float);
            writer.Stream.WriteSingle(Value);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("digit");
            writer.WriteString("{"); writer.WriteString(Value.ToString(CultureInfo.InvariantCulture)); writer.WriteString("}");

            if (writer.Debug)
                writer.WriteString(" // mFloat");

            writer.SetNeedNewLine();
        }
    }
}
