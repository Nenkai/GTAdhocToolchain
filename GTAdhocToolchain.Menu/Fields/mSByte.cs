using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mSByte: {Name} ({Value})")]
#pragma warning disable IDE1006 // Naming Styles
public class mSByte : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public sbyte Value { get; set; }

    public override void Read(MBinaryIO io)
    {
        Value = io.Stream.ReadSByte();
    }

    public override void Read(MTextIO io)
    {
        var numbToken = io.GetNumberToken();
        if (sbyte.TryParse(numbToken, out sbyte val))
            Value = val;
        else
            throw new UISyntaxError($"Unexpected sbyte token for mSByte. Got {numbToken}.");

        string end = io.GetToken();
        if (end != MTextIO.SCOPE_END.ToString())
            throw new UISyntaxError($"Expected mSByte scope end ({MTextIO.SCOPE_END}), got {end}");
    }

    public override void Write(MBinaryWriter writer)
    {
        writer.Stream.WriteVarInt((int)FieldType.SByte);
        writer.Stream.WriteSByte(Value);
    }

    public override void WriteText(MTextWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteSpace();
        writer.WriteString("digit");
        writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

        if (writer.Debug)
            writer.WriteString(" // mSByte");

        writer.SetNeedNewLine();
    }
}
