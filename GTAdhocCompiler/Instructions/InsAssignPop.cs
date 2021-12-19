using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Pops 2 pointers off the stack, and copies the second item to the first item
    /// </summary>
    public class InsAssignPop : InstructionBase
    {
        public override string InstructionName => "ASSIGN_POP";
    }
}
