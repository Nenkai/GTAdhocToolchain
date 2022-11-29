using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents an evaluation for an iterator.
    /// </summary>
    public class InsEval : InstructionBase
    {
        public readonly static InsEval Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.EVAL;

        public override string InstructionName => "EVAL";

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
