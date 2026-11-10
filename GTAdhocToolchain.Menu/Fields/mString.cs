//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System.Diagnostics;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mString: {Name} ({String})")]
#pragma warning disable IDE1006 // Naming Styles
public class mString : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public string? String { get; set; }

    public override void Read(MBinaryIO io)
    {
        String = io.Stream.Read7BitString();
    }

    public override void Read(MTextIO io)
    {
        String = io.GetString();

        string? end = io.GetToken();
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

        writer.WriteString("{\""); 
        if (!string.IsNullOrEmpty(String))
            writer.WriteString(String); 
        writer.WriteString("\"}");

        writer.SetNeedNewLine();
    }
}
