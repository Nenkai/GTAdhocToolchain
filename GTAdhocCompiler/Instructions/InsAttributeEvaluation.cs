using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Evaluates an attribute, puts or gets it into the specified variable storage index. Will push the value onto the stack.
    /// </summary>
    public class InsAttributeEvaluation : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_EVAL;

        public override string InstructionName => "ATTRIBUTE_EVAL";

        public List<AdhocSymbol> AttributeSymbols { get; set; } = new();
    }
}
