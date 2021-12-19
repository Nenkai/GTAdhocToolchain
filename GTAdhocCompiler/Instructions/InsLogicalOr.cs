using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Logical or instruction.
    /// </summary>
    public class InsLogicalOr : InstructionBase
    {
        public override string InstructionName => "LOGICAL_OR";

        /// <summary>
        /// Index to jump to if the first operand before the comparison matches.
        /// </summary>
        public int InstructionJumpIndex { get; set; }
    }
}
