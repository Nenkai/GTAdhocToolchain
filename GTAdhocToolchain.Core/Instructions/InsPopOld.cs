using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pops 1 pointer off the stack.
    /// </summary>
    public class InsPopOld : InstructionBase
    {
        public readonly static InsPopOld Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.POP_OLD;

        public override string InstructionName => "POP_OLD";

        public override void Serialize(AdhocStream stream)
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}";

    }
}
