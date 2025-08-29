using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core.Instructions;

using Esprima.Ast;

namespace GTAdhocToolchain.Core;

public class LoopContext : ScopeContext
{
    /// <summary>
    /// Direct jumps for continues
    /// </summary>
    public List<InsJump> ContinueJumps { get; set; } = [];

    /// <summary>
    /// Direct jumps for breaks to exit loops
    /// </summary>
    public List<InsJump> BreakJumps { get; set; } = [];

    public LoopContext(Node srcNode)
        : base(srcNode)
    {
        
    }
}
