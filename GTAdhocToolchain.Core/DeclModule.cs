using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

/// <summary>
/// Represents a module (or class) declaration.
/// </summary>
public class DeclModule : DeclValue
{
    /// <summary>
    /// Variables defined within this module or class.
    /// </summary>
    public Dictionary<string, DeclValue> Variables { get; set; } = [];

    public DeclModule(DeclModule? parent, string name, AdhocVariableType varType, Location? location)
        : base(parent, name, varType, location)
    {
        Name = name;
    }

    // GT7 1.00: 30E02D0 (mDeclModule::InsertNew)
    public void AddVariable(string name, DeclValue value)
    {
        Variables.Add(name, value);
    }

    public override void Dump(StringBuilder sb, int depth = 0)
    {
        sb.Append(' ', depth * 2);
        sb.Append($"@{VariableTypeToKeyword[Type]} {Name} {{\n");
        foreach (var (k, v) in Variables)
        {
            v.Dump(sb, depth + 1);
        }

        sb.Append(' ', depth * 2);
        sb.Append("}\n");
    }
}
