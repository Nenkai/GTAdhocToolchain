using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mBool: {Name} ({Value})")]
    public class mBool : mTypeBase
    {
        public bool Value { get; set; }

        public override void Read(MBinaryIO io)
        {
            Value = io.Stream.ReadBoolean();
        }

        public override void Read(MTextIO io)
        {
            var numbToken = io.GetNumberToken();
            if (numbToken == "1")
                Value = true;
            else if (numbToken == "0")
                Value = false;
            else
                throw new UISyntaxError($"Expected bool token (1/0) for mBool. Got {numbToken}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mBool scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.Bool);
            writer.Stream.WriteBoolean(Value);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("digit");
            writer.WriteString("{"); writer.WriteString(Value ? "1" : "0"); writer.WriteString("}");

            if (writer.Debug)
                writer.WriteString(" // mBool");

            writer.SetNeedNewLine();
        }
    }
}
