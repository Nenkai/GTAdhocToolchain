using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class Ins71 : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNK_71;

        public override string InstructionName => "UNK_71";

        public int Value { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadInt32();
        }

        public override string ToString()
            => InstructionType.ToString();
    }
}
