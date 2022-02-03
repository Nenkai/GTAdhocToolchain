using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields
{
    [DebuggerDisplay("mString: {Name} ({String})")]
    public class mString : mTypeBase
    {
        public string String { get; set; }

        public override void Read(MBinaryIO io)
        {
            String = io.Stream.Read7BitString();
        }

        public override void Read(MTextIO io)
        {
            String = io.GetString();

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected string scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.String);
            writer.Stream.WriteVarString(String);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("string");
            writer.WriteString("{\""); writer.WriteString(String); writer.WriteString("\"}");
            writer.SetNeedNewLine();
        }
    }
}
