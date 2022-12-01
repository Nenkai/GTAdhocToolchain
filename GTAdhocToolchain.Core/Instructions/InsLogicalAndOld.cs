using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Logical and instruction.
    /// </summary>
    public class InsLogicalAndOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LOGICAL_AND_OLD;

        public override string InstructionName => "LOGICAL_AND_OLD";

        /// <summary>
        /// Index to jump to if the first operand before the comparison matches.
        /// </summary>
        public int InstructionJumpIndex { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(InstructionJumpIndex);
        }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionJumpIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: Jump={InstructionJumpIndex}";
    }
}
