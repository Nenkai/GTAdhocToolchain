// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esprima;

namespace GTAdhocToolchain.Core.Variables;

public class Variable
{
    public int StackIndex { get; set; } = -1;
    public AdhocSymbol Symbol { get; set; }
    public AdhocVariableType Type { get; set; } = AdhocVariableType.Unknown;
    public int DeclarationLineNumber { get; set; }
    public string? DeclarationSourceFileName { get; set; }

    public Variable(AdhocSymbol symbol, AdhocVariableType variableType, int stackIndex, Location? location = null)
    {
        Symbol = symbol;
        Type = variableType;
        StackIndex = stackIndex;

        if (location is not null)
        {
            DeclarationLineNumber = location.Value.Start.Line;
            DeclarationSourceFileName = location.Value.Source;
        }
    }

    public Variable(AdhocSymbol symbol, AdhocVariableType variableType, int stackIndex, int sourceLineNumber, string? sourceFileName)
    {
        Symbol = symbol;
        Type = variableType;
        StackIndex = stackIndex;
        DeclarationLineNumber = sourceLineNumber;
        DeclarationSourceFileName = sourceFileName;
    }

    public override string ToString()
    {
        return $"Variable: {Symbol}";
    }
}
