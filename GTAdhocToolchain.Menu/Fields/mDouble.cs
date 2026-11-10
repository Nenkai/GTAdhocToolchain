//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System.Globalization;
using System.Diagnostics;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mDouble: {Name} ({Value})")]
#pragma warning disable IDE1006 // Naming Styles
public class mDouble : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
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
