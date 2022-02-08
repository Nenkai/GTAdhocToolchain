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
    [DebuggerDisplay("mExternalRef: {Name} ({Value})")]
    public class mExternalRef : mTypeBase
    {
        public string ExternalRefName { get; set; }

        public override void Read(MBinaryIO io)
        {
            ExternalRefName = io.Stream.Read7BitString();
        }

        public override void Read(MTextIO io)
        {
            ExternalRefName = io.GetString();

            string end = io.GetToken();
            if (end != MTextIO.SCOPE_END.ToString())
                throw new UISyntaxError($"Expected external ref scope end ({MTextIO.SCOPE_END}), got {end}");
        }

        public override void Write(MBinaryWriter writer)
        {
            writer.Stream.WriteVarInt((int)FieldType.ExternalRef);
            writer.Stream.WriteVarString(ExternalRefName);
        }

        public override void WriteText(MTextWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteSpace();
            writer.WriteString("ExternalRef");
            writer.WriteString("{\""); writer.WriteString(ExternalRefName); writer.WriteString("\"}");
            writer.SetNeedNewLine();
        }
    }
}
