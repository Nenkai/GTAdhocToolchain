// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Evaluates a symbol, puts or gets it into the specified variable storage index. Will push the value onto the stack.
/// </summary>
public class InsVariableEvaluation : InstructionBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.VARIABLE_EVAL;

    public override string InstructionName => "VARIABLE_EVAL";

    public List<AdhocSymbol> Path { get; set; } = [];

    public int VariableStorageIndex { get; set; }
    public bool IsStatic => Path.Count > 1;

    public InsVariableEvaluation(int index)
    {
        VariableStorageIndex = index;
    }

    public InsVariableEvaluation()
    {

    }

    public override void Serialize(AdhocStream stream)
    {
        stream.WriteSymbols(Path);
        stream.WriteInt32(VariableStorageIndex);
    }

    public override void Deserialize(AdhocStream stream)
    {
        Path = stream.ReadSymbols();
        VariableStorageIndex = stream.ReadInt32();
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
    {
        if (asCompareMode)
        {
            if (IsStatic)
                return $"{InstructionType}: {string.Join(',', Path.Select(e => e.Name.Split("#")[0]))}, Static:{VariableStorageIndex}";
            else
                return $"{InstructionType}: {string.Join(',', Path.Select(e => e.Name.Split("#")[0]))}, Local:{VariableStorageIndex}";
        }
        else
        {
            if (IsStatic)
                return $"{InstructionType}: {string.Join(',', Path.Select(e => e.Name))}, Static:{VariableStorageIndex}";
            else
                return $"{InstructionType}: {string.Join(',', Path.Select(e => e.Name))}, Local:{VariableStorageIndex}";
        }
    }
}
