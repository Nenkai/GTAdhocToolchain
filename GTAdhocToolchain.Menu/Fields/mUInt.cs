using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;
namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mUInt: {Name} ({Value})")]
#pragma warning disable IDE1006 // Naming Styles
public class mUInt : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public uint Value { get; set; }

    public override void Read(MBinaryIO io)
    {
        Value = io.Stream.ReadUInt32();
    }

    public override void Read(MTextIO io)
    {
        var numbToken = io.GetNumberToken();
        if (uint.TryParse(numbToken, out uint val))
            Value = val;
        else
            throw new UISyntaxError($"Unexpected token for mUInt. Got {numbToken}.");

        string end = io.GetToken();
        if (end != MTextIO.SCOPE_END.ToString())
            throw new UISyntaxError($"Expected mUInt scope end ({MTextIO.SCOPE_END}), got {end}");
    }

    public override void Write(MBinaryWriter writer)
    {
        writer.Stream.WriteVarInt((int)FieldType.UInt);
        writer.Stream.WriteUInt32(Value);
    }

    public override void WriteText(MTextWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteSpace();
        writer.WriteString("digit");
        writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

        if (writer.Debug)
            writer.WriteString(" // mUInt");

        writer.SetNeedNewLine();
    }
}
