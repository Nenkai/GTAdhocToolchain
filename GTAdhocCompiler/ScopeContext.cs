using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esprima.Ast;

namespace GTAdhocCompiler
{
    public class ScopeContext
    {
        /// <summary>
        /// Declared variables for this scope.
        /// </summary>
        public Dictionary<string, AdhocSymbol> ScopeVariables { get; set; } = new();

        public Node SourceNode { get; set; }

        public ScopeContext(Node node)
        {
            SourceNode = node;
        }
    }
}
