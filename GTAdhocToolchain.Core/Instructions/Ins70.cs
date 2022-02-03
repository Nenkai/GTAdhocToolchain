using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class Ins70 : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNK_70;

        public override string InstructionName => "UNK_70";

        public int InstructionIndex { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionIndex = stream.ReadInt32();
        }

        public override string ToString()
            => $"{InstructionType}: Jump To {InstructionIndex}";
    }
}
