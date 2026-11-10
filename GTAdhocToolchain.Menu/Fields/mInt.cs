//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System.Diagnostics;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mInt: {Name} ({Value})")]
#pragma warning disable IDE1006 // Naming Styles
public class mInt : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public int Value { get; set; }

    public override void Read(MBinaryIO io)
    {
        Value = io.Stream.ReadInt32();
    }

    public override void Read(MTextIO io)
    {
        var numbToken = io.GetNumberToken();
        if (int.TryParse(numbToken, out int val))
            Value = val;
        else
            throw new UISyntaxError($"Unexpected int token for mInt. Got {numbToken}.");

        string? end = io.GetToken();
        if (end != MTextIO.SCOPE_END.ToString())
            throw new UISyntaxError($"Expected mInt scope end ({MTextIO.SCOPE_END}), got {end}");
    }

    public override void Write(MBinaryWriter writer)
    {
        writer.Stream.WriteVarInt((int)FieldType.Int);
        writer.Stream.WriteInt32(Value);
    }

    public override void WriteText(MTextWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteSpace();
        writer.WriteString("digit");
        writer.WriteString("{"); writer.WriteString(Value.ToString()); writer.WriteString("}");

        if (writer.Debug)
            writer.WriteString(" // mInt");

        writer.SetNeedNewLine();
    }
}
