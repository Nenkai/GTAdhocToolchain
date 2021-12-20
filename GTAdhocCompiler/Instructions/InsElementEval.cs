using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents an array or map element evaluation instruction.
    /// </summary>
    public class InsElementEval : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ELEMENT_EVAL;

        public override string InstructionName => "ELEMENT_EVAL";
    }
}
