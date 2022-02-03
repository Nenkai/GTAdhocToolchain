using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using GTAdhocToolchain.Core; 

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mULong: {Name} ({Value})")]
    public class mULong : mTypeBase
    {
        public ulong Value { get; set; }

        public override void Read(MBinaryIO io)
        {
            Value = io.Stream.ReadUInt64();
        }

        public override void Read(MTextIO io)
        {
            var numbToken = io.GetNumberToken();
            if (ushort.TryParse(numbToken, out ushort val))
                Value = val;
            else
                throw new UISyntaxError($"Unexpected ushort token for mUShort. Got {numbToken}.");

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected mUShort scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.ULong);
            writer.Stream.WriteUInt64(Value);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("digit");
            writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

            if (writer.Debug)
                writer.WriteString(" // mULong");

            writer.SetNeedNewLine();
        }
    }
}
