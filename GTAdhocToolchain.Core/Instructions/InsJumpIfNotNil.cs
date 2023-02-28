using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsJumpIfNotNil : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP_IF_NOT_NIL;

        public override string InstructionName => "JUMP_IF_NOT_NIL";

        public int InstructionIndex { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(InstructionIndex);
        }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: Jump To {InstructionIndex}";
    }
}
