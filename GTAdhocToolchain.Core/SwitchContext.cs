using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core.Instructions;

using Esprima.Ast;

namespace GTAdhocToolchain.Core
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
