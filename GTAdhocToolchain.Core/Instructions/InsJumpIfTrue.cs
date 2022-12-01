using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a jump to instruction if true instruction. 
    /// Mostly used for switch cases.
    /// </summary>
    public class InsJumpIfTrue : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP_IF_TRUE;

        public override string InstructionName => "JUMP_IF_TRUE";

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
