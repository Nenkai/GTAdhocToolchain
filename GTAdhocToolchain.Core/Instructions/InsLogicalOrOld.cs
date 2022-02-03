using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Logical or instruction.
    /// </summary>
    public class InsLogicalOrOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LOGICAL_OR_OLD;

        public override string InstructionName => "LOGICAL_OR_OLD";

        /// <summary>
        /// Index to jump to if the first operand before the comparison matches.
        /// </summary>
        public int InstructionJumpIndex { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionJumpIndex = stream.ReadInt32();
        }


        public override string ToString()
           => $"{InstructionType}: Jump={InstructionJumpIndex}";
    }
}
