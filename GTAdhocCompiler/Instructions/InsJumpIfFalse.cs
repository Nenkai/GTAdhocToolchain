using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a jump to instruction if false instruction.
    /// </summary>
    public class InsJumpIfFalse : InstructionBase
    {
        public int JumpIndex { get; set; }
    }
}
