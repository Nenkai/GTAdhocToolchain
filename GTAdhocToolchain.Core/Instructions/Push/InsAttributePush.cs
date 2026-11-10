//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Pushes a new variable into an object's attribute.
/// </summary>
public class InsAttributePush : InstructionBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_PUSH;

    public override string InstructionName => "ATTRIBUTE_PUSH";

    public List<AdhocSymbol> AttributeSymbols { get; set; } = [];

    public override void Serialize(AdhocStream stream)
    {
        if (stream.Version.VersionNumber <= 5)
        {
            stream.WriteSymbol(AttributeSymbols[0]);
        }
        else
        {
            stream.WriteSymbols(AttributeSymbols);
        }
    }

    public override void Deserialize(AdhocStream stream)
    {
        if (stream.Version.VersionNumber <= 5)
        {
            AdhocSymbol attrName = stream.ReadSymbol();
            AttributeSymbols = [attrName];
        }
        else
            AttributeSymbols = stream.ReadSymbols();
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
        => $"{InstructionType}: {string.Join(',', AttributeSymbols.Select(e => e.Name))}";
}
