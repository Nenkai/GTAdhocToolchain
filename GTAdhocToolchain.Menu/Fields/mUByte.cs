using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mUByte: {Name} ({Value})")]
    public class mUByte : mTypeBase
    {
        public byte Value { get; set; }

        public override void Read(MBinaryIO io)
        {
            Value = io.Stream.Read1Byte();
        }

        public override void Read(MTextIO io)
        {
            var numbToken = io.GetNumberToken();
            if (byte.TryParse(numbToken, out byte val))
                Value = val;
            else
                throw new UISyntaxError($"Unexpected token for mByte. Got {numbToken}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mByte scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.UByte);
            writer.Stream.WriteByte(Value);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("digit");
            writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

            if (writer.Debug)
                writer.WriteString(" // mUByte");

            writer.SetNeedNewLine();
        }
    }
}
