using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocCompiler.Instructions;

using Esprima.Ast;

namespace GTAdhocCompiler
{
    public class SwitchContext : ScopeContext
    {
        /// <summary>
        /// Direct jumps for breaks to exit cases
        /// </summary>
        public List<InsJump> BreakJumps { get; set; } = new();

        public SwitchContext(Node srcNode)
            : base(srcNode)
        {
            
        }
    }
}
