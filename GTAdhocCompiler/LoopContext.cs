using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocCompiler.Instructions;

using Esprima.Ast;

namespace GTAdhocCompiler
{
    public class LoopContext
    {
        public Statement SourceLoopStatement { get; set; }

        /// <summary>
        /// Direct jumps for continues
        /// </summary>
        public List<InsJump> ContinueJumps { get; set; } = new();

        /// <summary>
        /// Diurect jumps for breaks to exit loops
        /// </summary>
        public List<InsJump> BreakJumps { get; set; } = new();

        public LoopContext(Statement loopNode)
        {
            SourceLoopNode = loopNode;
        }
    }
}
