using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsTryCatch : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.TRY_CATCH;

        public override string InstructionName => "TRY_CATCH";

        public int InstructionIndex { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionIndex = stream.ReadInt32();
        }

        public override string ToString()
           => $"{InstructionType}: {InstructionIndex}";
    }
}
