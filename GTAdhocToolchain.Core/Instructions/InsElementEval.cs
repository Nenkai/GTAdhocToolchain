using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents an array or map element evaluation instruction.
    /// </summary>
    public class InsElementEval : InstructionBase
    {
        public readonly static InsElementEval Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ELEMENT_EVAL;

        public override string InstructionName => "ELEMENT_EVAL";

        public override void Deserialize(AdhocStream stream)
        {
            
        }

        public override string ToString()
            => $"{InstructionType}";
    }
}
