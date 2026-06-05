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

    public DeclModule(DeclModule parent, string name, AdhocVariableType varType)
        : base(parent, name, varType)
    {
        Name = name;
    }

    // GT7 1.00: 30E02D0 (mDeclModule::InsertNew)
    public void AddVariable(string name, DeclValue value)
    {
        Variables.Add(name, value);
    }
}
