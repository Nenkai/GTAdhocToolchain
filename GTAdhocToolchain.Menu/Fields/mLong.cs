using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mLong: {Name} ({Value})")]
#pragma warning disable IDE1006 // Naming Styles
public class mLong : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public long Value { get; set; }

    public override void Read(MBinaryIO io)
    {
        Value = io.Stream.ReadInt64();
    }

    public override void Read(MTextIO io)
    {
        var numbToken = io.GetNumberToken();
        if (long.TryParse(numbToken, out long val))
            Value = val;
        else
            throw new UISyntaxError($"Unexpected long token for mLong. Got {numbToken}.");

        string end = io.GetToken();
        if (io.GetToken() != MTextIO.SCOPE_END.ToString())
            throw new UISyntaxError($"Expected mLong scope end ({MTextIO.SCOPE_END}), got {end}");
    }

    public override void Write(MBinaryWriter writer)
    {
        writer.Stream.WriteVarInt((int)FieldType.Long);
        writer.Stream.WriteInt64(Value);
    }

    public override void WriteText(MTextWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteSpace();
        writer.WriteString("digit");
        writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

        if (writer.Debug)
            writer.WriteString(" // mLong");

        writer.SetNeedNewLine();
    }
}
