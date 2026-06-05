using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esprima.Ast;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Core.Variables;

namespace GTAdhocToolchain.Core;

public class ScopeContext
{
    /// <summary>
    /// Variables declared in this scope.
    /// </summary>
    public Dictionary<AdhocSymbol, Variable> Variables { get; set; } = [];

    /// <summary>
    /// Type of scope.
    /// </summary>
    public AdhocScopeType Type { get; set; } = AdhocScopeType.Normal;

    /// <summary>
    /// Number of locals declared in this scope.
    /// </summary>
    public int NumLocals { get; set; }

    public int StackCounter { get; set; }

    /// <summary>
    /// Whether to emit a LEAVE instruction on scope exit.
    /// </summary>
    public bool CleanupOnExit { get; set; } = false;
}

public enum AdhocScopeType
{
    /// <summary>
    /// Regular scope.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Scope is top level frame.
    /// </summary>
    TopLevel = 1,

    /// <summary>
    /// Scope is also a module or class definition.
    /// </summary>
    ModuleOrClass = 2,
}
