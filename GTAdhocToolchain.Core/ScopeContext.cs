using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esprima.Ast;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Core;

public class ScopeContext
{
    /// <summary>
    /// Declared local variables for this scope.
    /// </summary>
    public Dictionary<string, AdhocSymbol> LocalScopeVariables { get; set; } = [];

    /// <summary>
    /// Declared static variables for this scope.
    /// This is used to clean up static references on module leaves, so that some static references don't conflict.
    /// i.e: Root "hidden.visible".
    /// </summary>
    public Dictionary<string, AdhocSymbol> StaticScopeVariables { get; set; } = [];

    public Node SourceNode { get; set; }

    public ScopeContext(Node node)
    {
        SourceNode = node;
    }
}
