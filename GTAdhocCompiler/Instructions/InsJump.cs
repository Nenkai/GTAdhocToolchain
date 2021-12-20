using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Jump to instruction index instruction.
    /// </summary>
    public class InsJump : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP;

        public override string InstructionName => "JUMP";

        public int JumpInstructionIndex { get; set; }
    }
}
