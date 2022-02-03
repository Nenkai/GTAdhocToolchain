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
    [DebuggerDisplay("mDouble: {Name} ({Value})")]
    public class mDouble : mTypeBase
    {
        public double Value { get; set; }

        public override void Read(MBinaryIO io)
        {
            Value = io.Stream.ReadDouble();
        }

        public override void Read(MTextIO io)
        {
            throw new NotImplementedException();
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.Float);
            writer.Stream.WriteDouble(Value);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("digit");
            writer.WriteString("{"); writer.WriteString(Value.ToString(CultureInfo.InvariantCulture)); writer.WriteString("}");

            if (writer.Debug)
                writer.WriteString(" // mDouble");

            writer.SetNeedNewLine();
        }
    }
}
