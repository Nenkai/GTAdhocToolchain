using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Jump to instruction index instruction.
    /// </summary>
    public class InsJump : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP;

        public override string InstructionName => "JUMP";

        public int JumpInstructionIndex { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            JumpInstructionIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: JumpTo={JumpInstructionIndex}";
    }
}
