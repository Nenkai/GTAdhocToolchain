using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mShort: {Name} ({Value})")]
    public class mShort : mTypeBase
    {
        public short Value { get; set; }

        public override void Read(MBinaryIO io)
        {
            Value = io.Stream.ReadInt16();
        }

        public override void Read(MTextIO io)
        {
            var numbToken = io.GetNumberToken();
            if (short.TryParse(numbToken, out short val))
                Value = val;
            else
                throw new UISyntaxError($"Unexpected short token for mShort. Got {numbToken}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mShort scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.Short);
            writer.Stream.WriteInt16(Value);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("digit");
            writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

            if (writer.Debug)
                writer.WriteString(" // mShort");

            writer.SetNeedNewLine();
        }
    }
}
