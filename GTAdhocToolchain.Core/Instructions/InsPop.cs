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
    public class InsPop : InstructionBase
    {
        public readonly static InsPop Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.POP;

        public override string InstructionName => "POP";

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
