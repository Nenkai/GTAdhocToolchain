using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsNop : InstructionBase
    {
        public static readonly InsNop Empty = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.NOP;

        public override string InstructionName => "NOP";

        public override void Deserialize(AdhocStream stream)
        {
            
        }

        public override string ToString()
            => $"{InstructionType}";

    }
}
