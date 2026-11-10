//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System.Text;

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Defines a new method within the scope. Pops all the arguments before the definition.
/// </summary>
public class InsMethodDefine : SubroutineBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.METHOD_DEFINE;

    public override string InstructionName => "METHOD_DEFINE";

    public InsMethodDefine() : base() { }
    public InsMethodDefine(AdhocVersion version)
    : base(version)
    {
    }

    public override void Serialize(AdhocStream stream)
    {
        stream.WriteSymbol(Name);
        CodeFrame.Write(stream);
    }

    public override void Deserialize(AdhocStream stream)
    {
        Name = stream.ReadSymbol();

        CodeFrame = new AdhocCodeFrame(stream.Version);
        CodeFrame.Read(stream);
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
    {
        var sb = new StringBuilder();
        sb.Append(InstructionType.ToString()).Append(" - ").Append(Name.Name);
        sb.Append(CodeFrame.Dissasemble());
        return sb.ToString();
    }
}
