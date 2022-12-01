using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a jump to instruction if false instruction. Pops the bool from the stack.
    /// Mostly used for if statements.
    /// </summary>
    public class InsJumpIfFalse : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP_IF_FALSE;

        public override string InstructionName => "JUMP_IF_FALSE";

        public int JumpIndex { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(JumpIndex);
        }

        public override void Deserialize(AdhocStream stream)
        {
            JumpIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: Jump To Func Ins {JumpIndex}";
    }
}
