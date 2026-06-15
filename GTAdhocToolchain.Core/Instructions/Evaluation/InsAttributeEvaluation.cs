// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Evaluates an attribute, puts or gets it into the specified variable storage index. Will push the value onto the stack.
/// </summary>
public class InsAttributeEvaluation : InstructionBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_EVAL;

    public override string InstructionName => "ATTRIBUTE_EVAL";

    public List<AdhocSymbol> Path { get; set; } = [];

    public override void Serialize(AdhocStream stream)
    {
        stream.WriteSymbols(Path);
    }

    public override void Deserialize(AdhocStream stream)
    {
        Path = stream.ReadSymbols();
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
        => $"{InstructionType}: {string.Join(',', Path.Select(e => e.Name))}";
}
